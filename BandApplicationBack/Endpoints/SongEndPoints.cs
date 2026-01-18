using System;
using Band.Shared.Domain;
using Band.Shared.Dto;
using BandApplicationBack.Domain;
using BandApplicationBack.Infrastructure.Hubs;
using BandApplicationBack.Infrastructure.Repositories;
using BandApplicationBack.State;
using Microsoft.AspNetCore.SignalR;

namespace BandApplicationBack.Endpoints
{
    public static class SongEndpoints
    {
        public static void MapSongEndpoints(this WebApplication app)
        {
            // --- DB-only song katalozi ostaju pod /songs ---
            var catalog = app.MapGroup("/songs");

            // SVE PESME (DB)
            catalog.MapGet(
                "/",
                async (SongRepository repository) => await repository.GetAllAsync()
            );

            // PESMA PO ID (DB)
            catalog.MapGet(
                "/{id:int}",
                async (int id, SongRepository repository) =>
                {
                    var song = await repository.GetByIdAsync(id);
                    return song is not null ? Results.Ok(song) : Results.NotFound();
                }
            );

            // DODAJ NOVU PESMU U DB
            catalog.MapPost(
                "/",
                async (Song song, SongRepository repository) =>
                {
                    var newSong = await repository.AddAsync(song);
                    return Results.Created($"/songs/{newSong.Id}", newSong);
                }
            );

            // OBRIŠI IZ DB
            catalog.MapDelete(
                "/{id:int}",
                async (int id, SongRepository repository) =>
                {
                    var existingSong = await repository.GetByIdAsync(id);
                    if (existingSong is null)
                        return Results.NotFound();

                    await repository.DeleteAsync(id);
                    return Results.NoContent();
                }
            );

            // --- Sve što menja "živo stanje" ide pod /sessions/{code}/songs ---
            var live = app.MapGroup("/sessions/{code}/songs");

            // TRENUTNI QUEUE (po sesiji)
            live.MapGet(
                "/queue",
                (string code) =>
                {
                    var s = SessionStateStore.Get(code);
                    return s is null
                        ? Results.NotFound("Session not found.")
                        : Results.Ok(s.QueueList);
                }
            );

            // ODABIR PESME (broadcast bez menjanja queue)
            live.MapPost(
                "/select/new/{id:int}",
                async (
                    string code,
                    int id,
                    IHubContext<BandAppHub> hubContext,
                    SongRepository repository
                ) =>
                {
                    var s = SessionStateStore.Get(code);
                    if (s is null)
                        return Results.BadRequest("Session not found.");

                    var selectedSong = await repository.GetByIdAsync(id);
                    if (selectedSong is null)
                        return Results.BadRequest("Song not found.");

                    var dto = new NextSongResponseDto
                    {
                        CurrentSong = selectedSong,
                        QueueList = s.QueueList,
                        PreviousSong = null,
                        IdNaredne = 0,
                    };

                    await hubContext
                        .Clients.Group(code)
                        .SendAsync(
                            SignalType.StateChanged,
                            new SongHubMessage { Type = MessageTypes.QueueUpdated, Payload = dto }
                        );

                    Touch(code, s);
                    return Results.Ok(dto);
                }
            );

            // DODAJ U QUEUE
            live.MapPost(
                "/queue/new/{id:int}",
                async (
                    string code,
                    int id,
                    IHubContext<BandAppHub> hubContext,
                    SongRepository repository
                ) =>
                {
                    var s = SessionStateStore.Get(code);
                    if (s is null)
                        return Results.BadRequest("Session not found.");

                    var selectedSong = await repository.GetByIdAsync(id);
                    if (selectedSong is null)
                        return Results.BadRequest("Song not found.");

                    var queued = new Song
                    {
                        ArtistId = selectedSong.ArtistId,
                        Id = selectedSong.Id,
                        Name = selectedSong.Name,
                        ArtistName = selectedSong.ArtistName,
                        Izvodjac = selectedSong.Izvodjac,
                        Sections = selectedSong.Sections,
                        Chords = selectedSong.Chords,
                        SongInListUniqueId = Guid.NewGuid(),
                    };

                    s.QueueList.Add(Qualify(queued));
                    var dto = new NextSongResponseDto
                    {
                        CurrentSong = s.QueueList.FirstOrDefault(x => x.IsActive),
                        QueueList = s.QueueList,
                        PreviousSong = null,
                        IdNaredne = (s.QueueList.Count > 1) ? s.QueueList[1].Id : 0,
                    };

                    await hubContext
                        .Clients.Group(code)
                        .SendAsync(
                            SignalType.StateChanged,
                            new SongHubMessage { Type = MessageTypes.QueueUpdated, Payload = dto }
                        );

                    Touch(code, s);
                    return Results.Ok(dto);
                }
            );

            // NEXT
            live.MapPost(
                "/queue/next",
                async (string code, IHubContext<BandAppHub> hubContext) =>
                {
                    var s = SessionStateStore.Get(code);
                    if (s is null)
                        return Results.BadRequest("Session not found.");
                    if (s.QueueList is null || s.QueueList.Count == 0)
                        return Results.BadRequest("Queue is empty.");

                    // Ako još nemamo trenutnu pesmu -> postavi prvu
                    if (!s.QueueList.Any(x => x.IsActive))
                    {
                        s.QueueList[0].IsActive = true;
                        var aktivna = s.QueueList[0];

                        var dtoInit = new NextSongResponseDto
                        {
                            CurrentSong = aktivna,
                            QueueList = s.QueueList,
                            PreviousSong = null,
                            IdNaredne = (s.QueueList.Count > 1) ? s.QueueList[1].Id : 0,
                        };

                        await hubContext
                            .Clients.Group(code)
                            .SendAsync(
                                SignalType.StateChanged,
                                new SongHubMessage
                                {
                                    Type = MessageTypes.NextSong,
                                    Payload = dtoInit,
                                }
                            );

                        Touch(code, s);
                        return Results.Ok(dtoInit);
                    }

                    var index = s.QueueList.FindIndex(x => x.IsActive);
                    if (index < 0)
                        return Results.BadRequest("Current song not found in queue.");
                    if (index == s.QueueList.Count - 1)
                        return Results.BadRequest("Already at the last song.");

                    s.QueueList[index].IsActive = false;
                    s.QueueList[index + 1].IsActive = true;

                    var dto = new NextSongResponseDto
                    {
                        CurrentSong = s.QueueList[index + 1],
                        QueueList = s.QueueList,
                        PreviousSong = s.QueueList[index],
                        IdNaredne = (index + 2 < s.QueueList.Count) ? s.QueueList[index + 2].Id : 0,
                    };

                    await hubContext
                        .Clients.Group(code)
                        .SendAsync(
                            SignalType.StateChanged,
                            new SongHubMessage { Type = MessageTypes.NextSong, Payload = dto }
                        );

                    Touch(code, s);
                    return Results.Ok(dto);
                }
            );

            // PREVIOUS
            live.MapPost(
                "/queue/previous",
                async (string code, IHubContext<BandAppHub> hubContext) =>
                {
                    var s = SessionStateStore.Get(code);
                    if (s is null)
                        return Results.BadRequest("Session not found.");
                    if (s.QueueList is null || s.QueueList.Count == 0)
                        return Results.BadRequest("Queue is empty.");

                    var index = s.QueueList.FindIndex(x => x.IsActive);
                    if (index < 0)
                        return Results.BadRequest("Current song not found in queue.");
                    if (index == 0)
                        return Results.BadRequest("Already at the first song.");

                    s.QueueList[index].IsActive = false;
                    s.QueueList[index - 1].IsActive = true;

                    var dto = new NextSongResponseDto
                    {
                        CurrentSong = s.QueueList[index - 1],
                        QueueList = s.QueueList,
                        PreviousSong = (index - 2 >= 0) ? s.QueueList[index - 2] : null,
                        IdNaredne = s.QueueList[index].Id,
                    };

                    await hubContext
                        .Clients.Group(code)
                        .SendAsync(
                            SignalType.StateChanged,
                            new SongHubMessage { Type = MessageTypes.NextSong, Payload = dto }
                        );

                    Touch(code, s);
                    return Results.Ok(dto);
                }
            );

            // REORDER
            live.MapPost(
                "/queue/reorder",
                async (
                    string code,
                    List<Guid> orderedSongIds,
                    IHubContext<BandAppHub> hubContext
                ) =>
                {
                    var s = SessionStateStore.Get(code);
                    if (s is null)
                        return Results.BadRequest("Session not found.");
                    if (s.QueueList is null || s.QueueList.Count == 0)
                        return Results.BadRequest("Queue is empty.");

                    var dict = s.QueueList.ToDictionary(x => x.SongInListUniqueId, x => x);
                    if (orderedSongIds.Any(id => !dict.ContainsKey(id)))
                        return Results.BadRequest("Invalid SongInListUniqueId in payload.");

                    var newOrder = orderedSongIds.Select(id => dict[id]).ToList();

                    var activeId = s.QueueList.FirstOrDefault(z => z.IsActive)?.SongInListUniqueId;
                    s.QueueList = newOrder;
                    foreach (var it in s.QueueList)
                        it.IsActive = (it.SongInListUniqueId == activeId);

                    var dto = new NextSongResponseDto
                    {
                        CurrentSong = s.QueueList.FirstOrDefault(x => x.IsActive),
                        QueueList = s.QueueList,
                        PreviousSong = null,
                        IdNaredne = 0,
                    };

                    await hubContext
                        .Clients.Group(code)
                        .SendAsync(
                            SignalType.StateChanged,
                            new SongHubMessage { Type = MessageTypes.QueueUpdated, Payload = dto }
                        );

                    Touch(code, s);
                    return Results.Ok(dto);
                }
            );
        }

        private static void Touch(string code, SessionState s)
        {
            s.LastTouched = DateTime.Now;
            SessionStateStore.Update(code, s);
        }

        // helper da ne deliš EF entitet direktno
        private static Song Qualify(Song s) =>
            new Song
            {
                ArtistId = s.ArtistId,
                Id = s.Id,
                Name = s.Name,
                ArtistName = s.ArtistName,
                Izvodjac = s.Izvodjac,
                Sections = s.Sections,
                Chords = s.Chords,
                SongInListUniqueId = s.SongInListUniqueId,
                IsActive = s.IsActive,
            };
    }
}

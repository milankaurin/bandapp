using Band.Shared.Domain;
using Band.Shared.Dto;
using BandApplicationBack.Domain;
using BandApplicationBack.Infrastructure;
using BandApplicationBack.Infrastructure.Hubs;
using BandApplicationBack.State;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BandApplicationBack.Endpoints
{
    public static class SessionEndpoints
    {
        public static void MapSessionEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/sessions");

            // KREIRAJ NOVU SESIJU
            group.MapPost(
                "/",
                async (BandAppDbContext db) =>
                {
                    string code;
                    // Generiši novi kod dok ne bude jedinstven
                    do
                    {
                        code = SessionCodeGenerator.New();
                    } while (SessionStateStore.Exists(code));

                    var session = new BandSession
                    {
                        Id = Guid.NewGuid(),
                        Code = code,
                        CreatedAt = DateTime.Now,
                        IsClosed = false,
                    };
                    //db.Add(session);
                    //await db.SaveChangesAsync();

                    SessionStateStore.Create(code);
                    return Results.Ok(new { code });
                }
            );

            // PRIDRUŽI SE
            group.MapPost(
                "/{code}/join",
                (string code) =>
                {
                    if (!SessionStateStore.Exists(code))
                    {
                        return Results.NotFound(
                            new { message = "Sesija ne postoji ili je zatvorena." }
                        );
                    }

                    return Results.Ok(new { message = "Uspešno pridruženo.", code });
                }
            );

            // CLOSE (zatvori sesiju, skini iz memorije)
            //group.MapPost(
            //    "/{code}/close",
            //    async (string code, BandAppDbContext db, IHubContext<BandAppHub> hub) =>
            //    {
            //        var entity = await db.Sessions.FirstOrDefaultAsync(x => x.Code == code);
            //        if (entity is null)
            //            return Results.NotFound("Session not found.");

            //        // 🔔 obavesti sve klijente
            //        await hub
            //            .Clients.Group(code)
            //            .SendAsync(
            //                SignalType.StateChanged,
            //                new SongHubMessage { Type = MessageTypes.SessionStopped }
            //            );

            //        // 🧠 izbaci iz memorije
            //        SessionStateStore.Remove(code);

            //        // 🗑 izbaci iz baze
            //        //db.Sessions.Remove(entity);
            //        //await db.SaveChangesAsync();

            //        return Results.Ok(new { message = "Sesija obrisana." });
            //    }
            //);

            group.MapPost(
                "/{code}/close",
                (string code) =>
                {
                    if (!SessionStateStore.Exists(code))
                        return Results.NotFound("Session not found.");

                    // ništa ne brišemo ovde
                    return Results.Ok(new { message = "Zahtev za izlazak iz sesije." });
                }
            );
        }
    }

    public static class SessionCodeGenerator
    {
        private static readonly char[] chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();

        private static readonly Random random = new();

        public static string New()
        {
            int length = random.Next(6, 11); // 6–10 (11 je exclusive)

            return new string(
                Enumerable.Range(0, length).Select(_ => chars[random.Next(chars.Length)]).ToArray()
            );
        }
    }
}

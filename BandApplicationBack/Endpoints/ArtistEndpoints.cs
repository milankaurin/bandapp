using Band.Shared.Domain;
using BandApplicationBack.Domain;
using BandApplicationBack.Infrastructure.Hubs;
using BandApplicationBack.Infrastructure.Repositories;
using Microsoft.AspNetCore.SignalR;

namespace BandApplicationBack.Endpoints
{
    public static class ArtistEndpoints
    {
        public static void MapArtistEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/artists");

            // Get all artists
            group.MapGet(
                "/",
                async (ArtistRepository repository) => await repository.GetAllAsync()
            );

            // Get artist by ID
            group.MapGet(
                "/{id:int}",
                async (int id, ArtistRepository repository) =>
                {
                    var artist = await repository.GetByIdAsync(id);
                    return artist is not null ? Results.Ok(artist) : Results.NotFound();
                }
            );

            // Add a new artist
            group.MapPost(
                "/",
                async (Artist artist, ArtistRepository repository) =>
                {
                    var newArtist = await repository.AddAsync(artist);
                    return Results.Created($"/artists/{newArtist.Id}", newArtist);
                }
            );

            // Update artist
            group.MapPut(
                "/{id:int}",
                async (int id, Artist updatedArtist, ArtistRepository repository) =>
                {
                    var existingArtist = await repository.GetByIdAsync(id);
                    if (existingArtist is null)
                        return Results.NotFound();

                    existingArtist.Name = updatedArtist.Name;
                    await repository.UpdateAsync(existingArtist);

                    return Results.Ok(existingArtist);
                }
            );

            // Delete artist
            group.MapDelete(
                "/{id:int}",
                async (int id, ArtistRepository repository) =>
                {
                    var existingArtist = await repository.GetByIdAsync(id);
                    if (existingArtist is null)
                        return Results.NotFound();

                    await repository.DeleteAsync(id);
                    return Results.NoContent();
                }
            );
        }
    }
}

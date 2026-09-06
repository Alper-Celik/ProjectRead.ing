// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
using Api.Auth.Utils;
using Api.Database;
using Api.Utils;
using Api.Works.Models;
using Api.Works.Queries;

using FairyBread;

using FluentValidation;

using NodaTime;

using Riok.Mapperly.Abstractions;

using static Api.Utils.ValidatorUtils;

namespace Api.Works.Mutations;


[MutationType]
public static partial class AddWorkMutations
{
    public static async Task<AddWorkPayload> AddWorkMutation(
            [Service] PGContext db,
            [Service] ICurrentUserId userId,
            CancellationToken ct,
            AddWorkInput input
            )
    {
        var work = AddWorkInputMapper.CreateFromDto(input, userId.Id!.Value, Now());

        await db.AddAsync(work, cancellationToken: ct);
        await db.SaveChangesAsync(cancellationToken: ct);

        return new AddWorkPayload(WorkMapper.ToDto(work));

    }
}

public record AddWorkPayload(
        Queries.Work Work
        );

public record AddWorkInput
{
    public required string Title { get; init; }

    public string? Description { get; init; }

    public Instant? WorkPublishedAt { get; init; }
    public Instant? WorkUpdatedAt { get; init; }
    public List<Queries.Work.WorkIdentifier> WorkIdentifiers { get; init; } = [];
    public required List<Guid> TagIds { get; init; } = [];
    public required List<Guid> AuthorIds { get; init; } = [];

    public class AddWorkInputValidator : AbstractValidator<AddWorkInput>, IRequiresOwnScopeValidator
    {
        public AddWorkInputValidator(PGContext db, ICurrentUserId userId)
        {
            RuleFor(w => w.TagIds).MustBeDistinct(nameof(TagIds));
            RuleFor(w => w.TagIds).IdsMustExist(db.WorkTags, userId.Id);


            RuleFor(w => w.AuthorIds).MustBeDistinct(nameof(AuthorIds));
            RuleFor(w => w.AuthorIds).IdsMustExist(db.Authors, userId.Id);
        }
    }
}
[Mapper]
public static partial class AddWorkInputMapper
{

    [MapperIgnoreTarget(nameof(Models.Work.RowVersion))]
    [MapperIgnoreSource(nameof(AddWorkInput.TagIds))]
    [MapperIgnoreSource(nameof(AddWorkInput.AuthorIds))]
    private static partial Models.Work CreateFromDtoInternal(AddWorkInput w, Guid id, Instant metadataAddedAt, Instant metadataUpdatedAt);


    public static Models.Work CreateFromDto(AddWorkInput w, Guid ownerId, Instant now)
    {
        var id = Guid.CreateVersion7().WithPostfix(Models.Work.IdPostfix);
        var work = CreateFromDtoInternal(w, id, now, now);
        work.OwnerId = ownerId;

        work.WorkTag_Works = [.. w.TagIds.Select(tId => new WorkTag_Work(id, tId))];
        work.Work_Authors = [.. w.AuthorIds.Select(aId => new Work_Author(id, aId))];


        return work;
    }



    [UserMapping(Default = true)]
    public static ZonedDateTime FromInstantToZonedDateTime(Instant i) => MapperUtils.FromInstantToZonedDateTime(i);

}
using System.Linq;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Language;
using Content.Server.Speech.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Language;
using Content.Shared.Language.Systems;
using Content.Shared.Mind.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Language.Components; //Delta-V - Banning humanoids from becoming ghost roles.
using Content.Shared.Language.Events;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed partial class MakeSentient : ReagentEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-make-sentient", ("chance", Probability));

    public override void Effect(ReagentEffectArgs args)
    {
        var entityManager = args.EntityManager;
        var uid = args.SolutionEntity;

        // Let affected entities speak normally to make this effect different from, say, the "random sentience" event
        // This also works on entities that already have a mind
        // We call this before the mind check to allow things like player-controlled mice to be able to benefit from the effect
        entityManager.RemoveComponent<ReplacementAccentComponent>(uid);
        entityManager.RemoveComponent<MonkeyAccentComponent>(uid);

        // Make sure the entity knows at least fallback (Galactic Common)
        var speaker = entityManager.EnsureComponent<LanguageSpeakerComponent>(uid);
        var knowledge = entityManager.EnsureComponent<LanguageKnowledgeComponent>(uid);
        var fallback = SharedLanguageSystem.FallbackLanguagePrototype;

        if (!knowledge.UnderstoodLanguages.Contains(fallback))
            knowledge.UnderstoodLanguages.Add(fallback);

        if (!knowledge.SpokenLanguages.Contains(fallback))
            knowledge.SpokenLanguages.Add(fallback);

        IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<LanguageSystem>().UpdateEntityLanguages(uid);

        // Stops from adding a ghost role to things like people who already have a mind
        if (entityManager.TryGetComponent<MindContainerComponent>(uid, out var mindContainer) && mindContainer.HasMind)
        {
            return;
        }

        // Don't add a ghost role to things that already have ghost roles
        if (entityManager.TryGetComponent(uid, out GhostRoleComponent? ghostRole))
        {
            return;
        }

        // Delta-V: Do not allow humanoids to become sentient. Intended to stop people from
        // repeatedly cloning themselves and using cognizine on their bodies.
        // HumanoidAppearanceComponent is common to all player species, and is also used for the
        // Ripley pilot whitelist, so there's a precedent for using it for this kind of check.
        if (entityManager.HasComponent<HumanoidAppearanceComponent>(uid))
        {
            return;
        }

        ghostRole = entityManager.AddComponent<GhostRoleComponent>(uid);
        entityManager.EnsureComponent<GhostTakeoverAvailableComponent>(uid);

        var entityData = entityManager.GetComponent<MetaDataComponent>(uid);
        ghostRole.RoleName = entityData.EntityName;
        ghostRole.RoleDescription = Loc.GetString("ghost-role-information-cognizine-description");
    }
}

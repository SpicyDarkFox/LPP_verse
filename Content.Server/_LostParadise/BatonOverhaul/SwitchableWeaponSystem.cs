using Content.Shared.Damage.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.SwitchableWeapon;
using Content.Shared.Toggleable;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;

namespace Content.Server.SwitchableWeapon;

public sealed class SwitchableWeaponSystem : EntitySystem
{
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SwitchableWeaponComponent, UseInHandEvent>(Toggle);
        SubscribeLocalEvent<SwitchableWeaponComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<SwitchableWeaponComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
        SubscribeLocalEvent<SwitchableWeaponComponent, ComponentAdd>(OnComponentAdded);
    }

    private void OnComponentAdded(EntityUid uid, SwitchableWeaponComponent component, ComponentAdd args)
    {
        UpdateState(uid, component);
    }

    private void OnGetMeleeDamage(EntityUid uid, SwitchableWeaponComponent component, ref GetMeleeDamageEvent args)
    {
        args.Damage = component.IsOpen ? component.DamageOpen : component.DamageFolded;
    }

    private void OnExamined(EntityUid uid, SwitchableWeaponComponent comp, ExaminedEvent args)
    {
        var msg = comp.IsOpen
            ? Loc.GetString("comp-switchable-examined-on")
            : Loc.GetString("comp-switchable-examined-off");
        args.PushMarkup(msg);
    }

    private void UpdateState(EntityUid uid, SwitchableWeaponComponent comp)
    {
        if (TryComp<ItemComponent>(uid, out var item))
        {
            _item.SetSize(uid, comp.IsOpen ? comp.SizeOpened : comp.SizeClosed, item);
            _item.SetHeldPrefix(uid, comp.IsOpen ? "on" : "off", component: item);
        }

        if (TryComp<AppearanceComponent>(uid, out var appearance))
            _appearance.SetData(uid, ToggleVisuals.Toggled, comp.IsOpen, appearance);

        if (TryComp<StaminaDamageOnHitComponent>(uid, out var stamComp))
        {
            stamComp.Damage = comp.IsOpen ? comp.StaminaDamageOpen : comp.StaminaDamageFolded;
        }
    }

    private void Toggle(EntityUid uid, SwitchableWeaponComponent comp, UseInHandEvent args)
    {
        comp.IsOpen = !comp.IsOpen;
        UpdateState(uid, comp);

        var soundToPlay = comp.IsOpen ? comp.OpenSound : comp.CloseSound;
        _audio.PlayPvs(soundToPlay, args.User);
    }
}
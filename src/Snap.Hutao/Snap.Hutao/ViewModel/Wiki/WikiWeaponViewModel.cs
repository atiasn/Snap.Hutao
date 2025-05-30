// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.ExceptionService;
using Snap.Hutao.Core.Logging;
using Snap.Hutao.Factory.ContentDialog;
using Snap.Hutao.Model.Calculable;
using Snap.Hutao.Model.Entity.Primitive;
using Snap.Hutao.Model.Intrinsic.Frozen;
using Snap.Hutao.Model.Metadata.Converter;
using Snap.Hutao.Model.Metadata.Item;
using Snap.Hutao.Model.Metadata.Weapon;
using Snap.Hutao.Service.Cultivation;
using Snap.Hutao.Service.Cultivation.Consumption;
using Snap.Hutao.Service.Hutao;
using Snap.Hutao.Service.Metadata;
using Snap.Hutao.Service.Metadata.ContextAbstraction;
using Snap.Hutao.Service.Notification;
using Snap.Hutao.Service.User;
using Snap.Hutao.UI.Xaml.Control.AutoSuggestBox;
using Snap.Hutao.UI.Xaml.Data;
using Snap.Hutao.UI.Xaml.View.Dialog;
using Snap.Hutao.Web.Response;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using CalculateBatchConsumption = Snap.Hutao.Web.Hoyolab.Takumi.Event.Calculate.BatchConsumption;
using CalculateClient = Snap.Hutao.Web.Hoyolab.Takumi.Event.Calculate.CalculateClient;

namespace Snap.Hutao.ViewModel.Wiki;

[ConstructorGenerated]
[Injection(InjectAs.Scoped)]
internal sealed partial class WikiWeaponViewModel : Abstraction.ViewModel
{
    private readonly IContentDialogFactory contentDialogFactory;
    private readonly ICultivationService cultivationService;
    private readonly ITaskContext taskContext;
    private readonly IMetadataService metadataService;
    private readonly IHutaoSpiralAbyssStatisticsCache hutaoCache;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly IInfoBarService infoBarService;
    private readonly IUserService userService;

    private WikiWeaponMetadataContext? metadataContext;

    public IAdvancedCollectionView<Weapon>? Weapons
    {
        get;
        set
        {
            if (field is not null)
            {
                field.CurrentChanged -= OnCurrentWeaponChanged;
            }

            SetProperty(ref field, value);

            if (value is not null)
            {
                value.CurrentChanged += OnCurrentWeaponChanged;
            }
        }
    }

    public BaseValueInfo? BaseValueInfo { get; set => SetProperty(ref field, value); }

    public ObservableCollection<SearchToken>? FilterTokens { get; set => SetProperty(ref field, value); }

    public string? FilterToken { get; set => SetProperty(ref field, value); }

    public FrozenDictionary<string, SearchToken>? AvailableTokens { get; private set; }

    /// <inheritdoc/>
    protected override async ValueTask<bool> LoadOverrideAsync()
    {
        if (await metadataService.InitializeAsync().ConfigureAwait(false))
        {
            try
            {
                metadataContext = await metadataService.GetContextAsync<WikiWeaponMetadataContext>().ConfigureAwait(false);

                List<Weapon> list = [.. metadataContext.Weapons.OrderByDescending(weapon => weapon.Sort)];

                await CombineComplexDataAsync(list, metadataContext).ConfigureAwait(false);

                using (await EnterCriticalSectionAsync().ConfigureAwait(false))
                {
                    IAdvancedCollectionView<Weapon> weaponsView = list.AsAdvancedCollectionView();

                    await taskContext.SwitchToMainThreadAsync();
                    Weapons = weaponsView;
                    Weapons.MoveCurrentToFirst();
                }

                FilterTokens = [];

                AvailableTokens = FrozenDictionary.ToFrozenDictionary(
                [
                    .. metadataContext.Weapons.Select((weapon, index) => KeyValuePair.Create(weapon.Name, new SearchToken(SearchTokenKind.Weapon, weapon.Name, index, sideIconUri: EquipIconConverter.IconNameToUri(weapon.Icon)))),
                    .. IntrinsicFrozen.FightPropertyNameValues.Select(nv => KeyValuePair.Create(nv.Name, new SearchToken(SearchTokenKind.FightProperty, nv.Name, (int)nv.Value))),
                    .. IntrinsicFrozen.ItemQualityNameValues.Select(nv => KeyValuePair.Create(nv.Name, new SearchToken(SearchTokenKind.ItemQuality, nv.Name, (int)nv.Value, quality: QualityColorConverter.QualityToColor(nv.Value)))),
                    .. IntrinsicFrozen.WeaponTypeNameValues.Select(nv => KeyValuePair.Create(nv.Name, new SearchToken(SearchTokenKind.WeaponType, nv.Name, (int)nv.Value, iconUri: WeaponTypeIconConverter.WeaponTypeToIconUri(nv.Value)))),
                ]);

                return true;
            }
            catch (OperationCanceledException)
            {
            }
        }

        return false;
    }

    private void OnCurrentWeaponChanged(object? sender, object? e)
    {
        UpdateBaseValueInfo(Weapons?.CurrentItem);
    }

    private async ValueTask CombineComplexDataAsync(List<Weapon> weapons, WikiWeaponMetadataContext context)
    {
        HutaoSpiralAbyssStatisticsMetadataContext context2 = await metadataService.GetContextAsync<HutaoSpiralAbyssStatisticsMetadataContext>().ConfigureAwait(false);
        await hutaoCache.InitializeForWikiWeaponViewAsync(context2).ConfigureAwait(false);
        ArgumentNullException.ThrowIfNull(hutaoCache.WeaponCollocations);

        foreach (Weapon weapon in weapons)
        {
            weapon.CollocationView = hutaoCache.WeaponCollocations.GetValueOrDefault(weapon.Id);
            weapon.CultivationItemsView ??= [.. weapon.CultivationItems.Select(i => context.IdMaterialMap.GetValueOrDefault(i, Material.Default))];
        }
    }

    [Command("CultivateCommand")]
    private async Task CultivateAsync(Weapon? weapon)
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Cultivate", "WikiAvatarViewModel.Command"));

        if (weapon is null)
        {
            return;
        }

        if (await userService.GetCurrentUserAndUidAsync().ConfigureAwait(false) is not { } userAndUid)
        {
            infoBarService.Warning(SH.MustSelectUserAndUid);
            return;
        }

        CultivatePromotionDeltaOptions deltaOptions;
        using (IServiceScope scope = serviceScopeFactory.CreateScope())
        {
            CalculableOptions options = new(null, weapon.ToCalculable());
            CultivatePromotionDeltaDialog dialog = await contentDialogFactory.CreateInstanceAsync<CultivatePromotionDeltaDialog>(scope.ServiceProvider, options).ConfigureAwait(false);
            (bool isOk, deltaOptions) = await dialog.GetPromotionDeltaAsync().ConfigureAwait(false);

            if (!isOk)
            {
                return;
            }
        }

        CalculateBatchConsumption? batchConsumption;
        using (IServiceScope scope = serviceScopeFactory.CreateScope())
        {
            CalculateClient calculateClient = scope.ServiceProvider.GetRequiredService<CalculateClient>();
            Response<CalculateBatchConsumption> response = await calculateClient.BatchComputeAsync(userAndUid, deltaOptions.Delta).ConfigureAwait(false);

            if (!ResponseValidator.TryValidate(response, scope.ServiceProvider, out batchConsumption))
            {
                return;
            }
        }

        LevelInformation levelInformation = LevelInformation.From(deltaOptions.Delta);
        try
        {
            InputConsumption input = new()
            {
                Type = CultivateType.Weapon,
                ItemId = weapon.Id,
                Items = batchConsumption.OverallConsume,
                LevelInformation = levelInformation,
                Strategy = deltaOptions.Strategy,
            };

            _ = await cultivationService.SaveConsumptionAsync(input).ConfigureAwait(false) switch
            {
                ConsumptionSaveResultKind.NoProject => infoBarService.Warning(SH.ViewModelCultivationEntryAddWarning),
                ConsumptionSaveResultKind.Skipped => infoBarService.Information(SH.ViewModelCultivationConsumptionSaveSkippedHint),
                ConsumptionSaveResultKind.NoItem => infoBarService.Information(SH.ViewModelCultivationConsumptionSaveNoItemHint),
                ConsumptionSaveResultKind.Added => infoBarService.Success(SH.ViewModelCultivationEntryAddSuccess),
                _ => default,
            };
        }
        catch (HutaoException ex)
        {
            infoBarService.Error(ex, SH.ViewModelCultivationAddWarning);
        }
    }

    private void UpdateBaseValueInfo(Weapon? weapon)
    {
        if (weapon is null || metadataContext is null)
        {
            BaseValueInfo = null;
            return;
        }

        BaseValueInfoMetadataContext context = new()
        {
            GrowCurveMap = metadataContext.LevelDictionaryWeaponGrowCurveMap,
            PromoteMap = metadataContext.IdDictionaryWeaponLevelPromoteMap[weapon.PromoteId],
        };

        BaseValueInfo = new(weapon.MaxLevel, weapon.GrowCurves.ToPropertyCurveValues(), context);
    }

    [Command("FilterCommand")]
    private void ApplyFilter()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Filter weapons", "WikiAvatarViewModel.Command"));

        if (Weapons is null)
        {
            return;
        }

        Weapons.Filter = FilterTokens is null or [] ? default! : WeaponFilter.Compile(FilterTokens);

        if (Weapons.CurrentItem is null)
        {
            Weapons.MoveCurrentToFirst();
        }
    }
}
// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.Logging;
using Snap.Hutao.Factory.ContentDialog;
using Snap.Hutao.Service;
using Snap.Hutao.Service.Notification;
using Snap.Hutao.UI.Xaml.View.Dialog;

namespace Snap.Hutao.ViewModel.Setting;

[ConstructorGenerated]
[Injection(InjectAs.Scoped)]
internal sealed partial class SettingGeetestViewModel : Abstraction.ViewModel
{
    private readonly IContentDialogFactory contentDialogFactory;
    private readonly IServiceProvider serviceProvider;
    private readonly IInfoBarService infoBarService;
    private readonly ITaskContext taskContext;
    private readonly AppOptions appOptions;

    [Command("ConfigureGeetestUrlCommand")]
    private async Task ConfigureGeetestUrlAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Config geetest url", "SettingGeetestViewModel.Command"));

        GeetestCustomUrlDialog dialog = await contentDialogFactory.CreateInstanceAsync<GeetestCustomUrlDialog>(serviceProvider).ConfigureAwait(false);
        (bool isOk, string template) = await dialog.GetUrlAsync().ConfigureAwait(false);

        if (!isOk)
        {
            return;
        }

        await taskContext.SwitchToMainThreadAsync();
        appOptions.GeetestCustomCompositeUrl = template;
        infoBarService.Success(SH.ViewModelSettingGeetestCustomUrlSucceed);
    }
}
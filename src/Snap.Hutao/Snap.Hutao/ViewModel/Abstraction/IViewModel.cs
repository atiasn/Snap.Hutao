// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.Abstraction;
using Snap.Hutao.UI.Xaml;

namespace Snap.Hutao.ViewModel.Abstraction;

internal interface IViewModel : IPageScoped, IResurrectable
{
    CancellationToken CancellationToken { get; set; }

    SemaphoreSlim CriticalSection { get; }

    IDeferContentLoader? DeferContentLoader { get; set; }

    bool IsViewUnloaded { get; set; }

    void Uninitialize();
}
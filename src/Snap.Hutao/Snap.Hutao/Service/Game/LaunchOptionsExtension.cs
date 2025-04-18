// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Model;
using Snap.Hutao.Model.Intrinsic;
using System.Collections.Immutable;

namespace Snap.Hutao.Service.Game;

internal static class LaunchOptionsExtension
{
    public static ImmutableArray<AspectRatio> SaveAspectRatio(this LaunchOptions options, AspectRatio aspectRatio)
    {
        if (!options.AspectRatios.Contains(aspectRatio))
        {
            options.AspectRatios = options.AspectRatios.Add(aspectRatio);
        }

        return options.AspectRatios;
    }

    public static ImmutableArray<AspectRatio> RemoveAspectRatio(this LaunchOptions options, AspectRatio aspectRatio)
    {
        if (aspectRatio.Equals(options.SelectedAspectRatio))
        {
            options.SelectedAspectRatio = default;
        }

        options.AspectRatios = options.AspectRatios.Remove(aspectRatio);
        return options.AspectRatios;
    }

    public static NameValue<PlatformType>? GetCurrentPlatformTypeForSelectionOrDefault(this LaunchOptions options)
    {
        return options.PlatformTypes.SingleOrDefault(c => c.Value == options.PlatformType);
    }
}
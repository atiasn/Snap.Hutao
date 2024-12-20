// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Model.Intrinsic;
using Snap.Hutao.Model.Metadata.Converter;

namespace Snap.Hutao.UI.Xaml.Data.Converter.Specialized;

internal sealed partial class ElementTypeIconConverter : ValueConverter<ElementType, Uri>
{
    public override Uri Convert(ElementType from)
    {
        return ElementNameIconConverter.ElementNameToUri(from.GetLocalizedDescription());
    }
}
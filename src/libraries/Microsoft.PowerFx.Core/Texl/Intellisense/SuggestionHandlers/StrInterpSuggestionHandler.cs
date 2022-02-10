﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Intellisense
{
    internal partial class Intellisense
    {
        internal sealed class StrInterpSuggestionHandler : NodeKindSuggestionHandler
        {
            public StrInterpSuggestionHandler()
                : base(NodeKind.StrInterp)
            {
            }

            internal override bool TryAddSuggestionsForNodeKind(IntellisenseData.IntellisenseData intellisenseData)
            {
                Contracts.AssertValue(intellisenseData);

                var curNode = intellisenseData.CurNode;
                var cursorPos = intellisenseData.CursorPos;

                var strInterpNode = curNode.AsStrInterp();
                var spanMin = strInterpNode.Token.Span.Min;

                if (cursorPos < spanMin)
                {
                    // Cursor is before the head
                    // i.e. | $"...."
                    // Suggest possibilities that can result in a value.
                    IntellisenseHelper.AddSuggestionsForValuePossibilities(intellisenseData, strInterpNode);
                }
                else if (strInterpNode.Token.Span.Lim > cursorPos || strInterpNode.StrInterpEnd == null)
                {
                    // Handling the erroneous case when user enters a space after $ and cursor is after space.
                    // Cursor is before the open quote.
                    // Eg: $ | "
                    return false;
                }
                else
                {
                    // If there was no close quote we would have an error node.
                    Contracts.Assert(strInterpNode.StrInterpEnd != null);

                    if (cursorPos <= strInterpNode.StrInterpEnd.Span.Min)
                    {
                        // Cursor position is before the close quote and there are no arguments.
                        // If there were arguments FindNode should have returned one of those.
                        if (intellisenseData.CurFunc != null && intellisenseData.CurFunc.MaxArity > 0)
                        {
                            IntellisenseHelper.AddSuggestionsForTopLevel(intellisenseData, strInterpNode);
                        }
                    }
                    else if (IntellisenseHelper.CanSuggestAfterValue(cursorPos, intellisenseData.Script))
                    {
                        // Verify that cursor is after a space after the closed parenthesis and
                        // suggest binary operators.
                        IntellisenseHelper.AddSuggestionsForAfterValue(intellisenseData, intellisenseData.Binding.GetType(strInterpNode));
                    }
                }

                return true;
            }
        }
    }
}

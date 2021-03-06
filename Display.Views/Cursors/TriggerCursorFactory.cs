﻿//------------------------------------------------------------------------------
// Copyright (C) 2017 Josi Coder

// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the Free
// Software Foundation, either version 3 of the License, or (at your option)
// any later version.

// This program is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
// FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for
// more details.

// You should have received a copy of the GNU General Public License along with
// this program. If not, see <http://www.gnu.org/licenses/>.
//--------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using PB = Praeclarum.Bind;
using ScopeLib.Utilities;
using ScopeLib.Sampling;
using ScopeLib.Display.ViewModels;
using ScopeLib.Display.Graphics;

namespace ScopeLib.Display.Views
{
    /// <summary>
    /// Creates cursors used on the scope screen for trigger-related values.
    /// </summary>
    internal static class TriggerCursorFactory
    {
        private const char _triggerSymbol = 'T';
        private const char _triggerTypeRisingSymbol = '\u2191';
        private const char _triggerTypeFallingSymbol = '\u2193';
        private const string _armedCaption = "A'd";
        private const string _triggeredCaption = "T'd";

        /// <summary>
        /// Creates a trigger criteria cursor for a level-based trigger.
        /// </summary>
        internal static BoundCursor CreateTriggerCriteriaCursor(
            LevelTriggerViewModel triggerVM,
            ChannelViewModel triggerChannelConfiguration,
            Func<double> referenceLevelProvider)
        {
            var triggerModeSymbol =
                triggerVM.Mode == LevelTriggerMode.RisingEdge ? _triggerTypeRisingSymbol
                : triggerVM.Mode == LevelTriggerMode.FallingEdge ? _triggerTypeFallingSymbol
                : '?';

            Func<ScopeCursor, ValueConverter<double, double>, PB.Binding> bindingProvider =
                (cursor, valueConverter) => PB.Binding.Create (() =>
                    cursor.Position.Y == valueConverter.DerivedValue &&
                    valueConverter.OriginalValue == triggerVM.Level);

            var influencingObjects = new INotifyPropertyChanged[]
            {
                triggerChannelConfiguration,
                triggerChannelConfiguration.ReferencePointPosition
            };

            return  CreateTriggerCriteriaCursor(
                triggerModeSymbol,
                () => triggerVM.Level,
                bindingProvider,
                () => triggerChannelConfiguration.YScaleFactor,
                () => triggerChannelConfiguration.ReferencePointPosition.Y,
                referenceLevelProvider,
                triggerVM.ChannelVM.BaseUnitString,
                triggerChannelConfiguration.Color,
                influencingObjects);
        }

        /// <summary>
        /// Creates a trigger criteria cursor for a level-based trigger.
        /// </summary>
        private static BoundCursor CreateTriggerCriteriaCursor(
            char triggerModeSymbol,
            Func<double> valueProvider,
            Func<ScopeCursor, ValueConverter<double, double>, PB.Binding> valueBindingProvider,
            Func<double> valueScaleFactorProvider,
            Func<double> referencePointPositionProvider,
            Func<double> referenceLevelProvider,
            string baseUnitString,
            Color levelColor,
            IEnumerable<INotifyPropertyChanged> influencingObjects)
        {
            var triggerCaption = string.Format("{0}{1}", _triggerSymbol, triggerModeSymbol);
            Func<String> levelTextProvider = () =>
                UnitHelper.BuildValueText(baseUnitString, valueProvider());

            var cairoColor = CairoHelpers.ToCairoColor(levelColor);

            var cursor = new ScopeCursor
            {
                Lines = ScopeCursorLines.Y,
                LineWeight = ScopeCursorLineWeight.Low,
                SelectableLines = ScopeCursorLines.Y,
                Markers = ScopeCursorMarkers.YFull,
                Color = cairoColor,
                Captions = new []
                {
                    new ScopePositionCaption(() => triggerCaption, ScopeHorizontalAlignment.Left, ScopeVerticalAlignment.Bottom, ScopeAlignmentReference.YPositionAndHorizontalRangeEdge, true, cairoColor),
                    new ScopePositionCaption(() => triggerCaption, ScopeHorizontalAlignment.Right, ScopeVerticalAlignment.Bottom, ScopeAlignmentReference.YPositionAndHorizontalRangeEdge, true, cairoColor),
                    new ScopePositionCaption(levelTextProvider, ScopeHorizontalAlignment.Right, ScopeVerticalAlignment.Top, ScopeAlignmentReference.YPositionAndHorizontalRangeEdge, true, cairoColor),
                },
            };

            // === Create value converters. ===

            var valueConverter = new ValueConverter<double, double>(
                val => (val - referenceLevelProvider()) * valueScaleFactorProvider() + referencePointPositionProvider(),
                val => ((val - referencePointPositionProvider()) / valueScaleFactorProvider()) + referenceLevelProvider());

            // === Create bindings. ===

            // Bind the cursor's position.
            var binding = valueBindingProvider(cursor, valueConverter);

            // The trigger cursor's position depends on some additional values (except the primary value
            // it is bound to). Update it if any of these values changes. ===
            influencingObjects.ForEachDo(influencingObject =>
            {
                influencingObject.PropertyChanged += (sender, e) =>
                {
                    PB.Binding.InvalidateMember(() => valueConverter.DerivedValue);
                };
            });

            return new BoundCursor(cursor, new [] {binding});
        }

        /// <summary>
        /// Creates a trigger point cursor.
        /// </summary>
        internal static BoundCursor CreateTriggerPointCursor(GraphbaseViewModel graphbaseVM)
        {
            var triggerVM = graphbaseVM.TriggerVM;

            Func<String> triggerStateCaptionProvider = () =>
                triggerVM.State == TriggerState.Armed ? _armedCaption
                : triggerVM.State == TriggerState.Triggered ? _triggeredCaption
                : "";

            Func<String> positionTextProvider = () =>
                UnitHelper.BuildValueText(graphbaseVM.BaseUnitString,
                    triggerVM.HorizontalPosition / graphbaseVM.ScaleFactor);

            var markerColor = CairoHelpers.ToCairoColor(graphbaseVM.Color);

            var cursor = new ScopeCursor
            {
                Lines = ScopeCursorLines.X,
                LineWeight = ScopeCursorLineWeight.Low,
                SelectableLines = ScopeCursorLines.X,
                Markers = ScopeCursorMarkers.XFull,
                Color = markerColor,
                Captions = new []
                {
                    new ScopePositionCaption(triggerStateCaptionProvider, ScopeHorizontalAlignment.Left, ScopeVerticalAlignment.Top, ScopeAlignmentReference.XPositionAndVerticalRangeEdge, true, markerColor),
                    new ScopePositionCaption(triggerStateCaptionProvider, ScopeHorizontalAlignment.Left, ScopeVerticalAlignment.Bottom, ScopeAlignmentReference.XPositionAndVerticalRangeEdge, true, markerColor),
                    new ScopePositionCaption(positionTextProvider, ScopeHorizontalAlignment.Right, ScopeVerticalAlignment.Top, ScopeAlignmentReference.XPositionAndVerticalRangeEdge, true, markerColor),
                },
            };

            // === Create bindings. ===

            // Bind the cursor's position.
            var binding = PB.Binding.Create (() => cursor.Position.X == triggerVM.HorizontalPosition);

            return new BoundCursor(cursor, new [] {binding});
        }
    }
}


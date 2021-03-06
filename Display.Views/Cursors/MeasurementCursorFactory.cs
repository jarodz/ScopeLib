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
using ScopeLib.Display.ViewModels;
using ScopeLib.Display.Graphics;

namespace ScopeLib.Display.Views
{
    /// <summary>
    /// Creates cursors used on the scope screen for measuring values.
    /// </summary>
    internal static class MeasurementCursorFactory
    {
        private const char _deltaSymbol = '\u2206';

        private enum MeasurementAxis : short
        {
            X,
            Y
        }

        /// <summary>
        /// Creates a cursor used for time measurements.
        /// </summary>
        internal static BoundCursor CreateTimeMeasurementCursor(
            MeasurementCursorViewModel cursorVM,
            GraphbaseViewModel graphbaseVM,
            bool isReferenceCursor,
            Func<double> deltaReferenceLevelProvider,
            Func<double> referenceLevelProvider)
        {
            Func<ScopeCursor, ValueConverter<double, double>, PB.Binding> bindingProvider =
                (cursor, valueConverter) => PB.Binding.Create (() =>
                    cursor.Position.X == valueConverter.DerivedValue &&
                    valueConverter.OriginalValue == cursorVM.Value);

            var influencingObjects = new INotifyPropertyChanged[]
            {
                graphbaseVM,
                graphbaseVM.TriggerVM,
            };

            return CreateMeasurementCursor(
                MeasurementAxis.X,
                isReferenceCursor,
                () => cursorVM.Value,
                bindingProvider,
                deltaReferenceLevelProvider,
                () => graphbaseVM.ScaleFactor,
                () => graphbaseVM.TriggerVM.HorizontalPosition,
                referenceLevelProvider,
                graphbaseVM.BaseUnitString,
                graphbaseVM.Color,
                influencingObjects);
        }

        /// <summary>
        /// Creates a cursor used for level measurements.
        /// </summary>
        internal static BoundCursor CreateLevelMeasurementCursor(
            MeasurementCursorViewModel cursorVM,
            ChannelViewModel cursorChannelConfiguration,
            bool isReferenceCursor,
            Func<double> deltaReferenceLevelProvider,
            Func<double> referenceLevelProvider)
        {
            Func<ScopeCursor, ValueConverter<double, double>, PB.Binding> bindingProvider =
                (cursor, valueConverter) => PB.Binding.Create (() =>
                    cursor.Position.Y == valueConverter.DerivedValue &&
                    valueConverter.OriginalValue == cursorVM.Value);
            
            var influencingObjects = new INotifyPropertyChanged[]
            {
                cursorChannelConfiguration,
                cursorChannelConfiguration.ReferencePointPosition
            };

            return CreateMeasurementCursor(
                MeasurementAxis.Y,
                isReferenceCursor,
                () => cursorVM.Value,
                bindingProvider,
                deltaReferenceLevelProvider,
                () => cursorChannelConfiguration.YScaleFactor,
                () => cursorChannelConfiguration.ReferencePointPosition.Y,
                referenceLevelProvider,
                cursorChannelConfiguration.BaseUnitString,
                cursorChannelConfiguration.Color,
                influencingObjects);
        }

        /// <summary>
        /// Creates a measurement cursor.
        /// </summary>
        private static BoundCursor CreateMeasurementCursor(
            MeasurementAxis measurementAxis,
            bool isReferenceCursor,
            Func<double> valueProvider,
            Func<ScopeCursor, ValueConverter<double, double>, PB.Binding> valueBindingProvider,
            Func<double> deltaMeasurementReferenceValueProvider,
            Func<double> valueScaleFactorProvider,
            Func<double> referencePointPositionProvider,
            Func<double> referenceValueProvider,
            string baseUnitString,
            Color cursorColor,
            IEnumerable<INotifyPropertyChanged> influencingObjects)
        {
            Func<String> basicValueTextProvider = () =>
                UnitHelper.BuildValueText(baseUnitString, valueProvider());

            Func<String> valueTextProvider;
            if (deltaMeasurementReferenceValueProvider == null)
            {
                valueTextProvider = basicValueTextProvider;
            }
            else
            {
                valueTextProvider = () =>
                    string.Format("{0} / {1} = {2}", basicValueTextProvider(), _deltaSymbol,
                        UnitHelper.BuildValueText(baseUnitString,
                            valueProvider() - deltaMeasurementReferenceValueProvider()));
            }

            var cursor =
                measurementAxis == MeasurementAxis.Y
                ? CreateYAxisMeasurementCursor (isReferenceCursor, valueTextProvider, cursorColor)
                : CreateXAxisMeasurementCursor (isReferenceCursor, valueTextProvider, cursorColor);

            // === Create value converters. ===

            var valueConverter = new ValueConverter<double, double>(
                val => (val - referenceValueProvider()) * valueScaleFactorProvider() + referencePointPositionProvider(),
                val => ((val - referencePointPositionProvider()) / valueScaleFactorProvider()) + referenceValueProvider());

            // === Create bindings. ===

            // Bind the cursor's position.
            var binding = valueBindingProvider(cursor, valueConverter);

            // The measurement cursor's position depends on some additional values (except the primary value
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
        /// Creates a vertical (X axis) measurement cursor.
        /// </summary>
        private static ScopeCursor CreateXAxisMeasurementCursor(bool isReferenceCursor,
            Func<String> valueTextProvider, Color cursorColor
        )
        {
            var cairoColor = CairoHelpers.ToCairoColor(cursorColor);

            ScopeCursorMarkers markers;
            ScopeHorizontalAlignment valueAlignment;
            if (isReferenceCursor)
            {
                markers = ScopeCursorMarkers.XLeft;
                valueAlignment = ScopeHorizontalAlignment.Right;
            }
            else
            {
                markers = ScopeCursorMarkers.XRight;
                valueAlignment = ScopeHorizontalAlignment.Left;
            }

            return new ScopeCursor
            {
                Lines = ScopeCursorLines.X,
                LineWeight = ScopeCursorLineWeight.Medium,
                SelectableLines = ScopeCursorLines.X,
                Markers = markers,
                Color = cairoColor,
                Captions = new []
                {
                    new ScopePositionCaption(valueTextProvider, valueAlignment, ScopeVerticalAlignment.Top, ScopeAlignmentReference.XPositionAndVerticalRangeEdge, true, cairoColor),
                },
            };
        }

        /// <summary>
        /// Creates a horizontal (Y axis) measurement cursor.
        /// </summary>
        private static ScopeCursor CreateYAxisMeasurementCursor(bool isReferenceCursor,
            Func<String> valueTextProvider, Color cursorColor
        )
        {
            var cairoColor = CairoHelpers.ToCairoColor(cursorColor);

            ScopeCursorMarkers markers;
            ScopeVerticalAlignment valueAlignment;
            if (isReferenceCursor)
            {
                markers = ScopeCursorMarkers.YLower;
                valueAlignment = ScopeVerticalAlignment.Top;
            }
            else
            {
                markers = ScopeCursorMarkers.YUpper;
                valueAlignment = ScopeVerticalAlignment.Bottom;
            }

            return new ScopeCursor
            {
                Lines = ScopeCursorLines.Y,
                LineWeight = ScopeCursorLineWeight.Medium,
                SelectableLines = ScopeCursorLines.Y,
                Markers = markers,
                Color = cairoColor,
                Captions = new []
                {
                    new ScopePositionCaption(valueTextProvider, ScopeHorizontalAlignment.Right, valueAlignment, ScopeAlignmentReference.YPositionAndHorizontalRangeEdge, true, cairoColor),
                },
            };
        }
    }
}


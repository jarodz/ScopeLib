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

namespace ScopeLib.Display.ViewModels
{
    /// <summary>
    /// Provides the viewmodel of a measurement cursor.
    /// </summary>
    public class MeasurementCursorViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes an instance of this class.
        /// </summary>
        internal MeasurementCursorViewModel ()
        {
            Value = 0.0;
            Visible = false;
        }

        private bool _visible;
        /// <summary>
        /// Gets or sets a value indicating whether the cursor is visible.
        /// </summary>
        public bool Visible
        {
            get
            {
                return _visible;
            }
            set
            {
                _visible = value;
                RaisePropertyChanged();
            }
        }

        private double _value;
        /// <summary>
        /// Gets or sets the cursor value.
        /// </summary>
        public double Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
                RaisePropertyChanged();
            }
        }
    }
}


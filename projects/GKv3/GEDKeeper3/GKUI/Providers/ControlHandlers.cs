﻿/*
 *  "GEDKeeper", the personal genealogical database editor.
 *  Copyright (C) 2009-2018 by Sergey V. Zhdanovskih.
 *
 *  This file is part of "GEDKeeper".
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using BSLib;
using Eto.Drawing;
using Eto.Forms;
using GKCore.Controllers;
using GKCore.Interfaces;
using GKUI.Components;

namespace GKUI.Providers
{
    public sealed class LabelHandler : ControlHandler<Label, LabelHandler>, ILabelHandler
    {
        public LabelHandler(Label control) : base(control)
        {
        }

        public string Text
        {
            get { return Control.Text; }
            set { Control.Text = value; }
        }
    }

    public sealed class ButtonHandler : ControlHandler<Button, ButtonHandler>, IButtonHandler
    {
        public ButtonHandler(Button control) : base(control)
        {
        }

        public bool Enabled
        {
            get { return Control.Enabled; }
            set { Control.Enabled = value; }
        }

        public string Text
        {
            get { return Control.Text; }
            set { Control.Text = value; }
        }
    }

    public sealed class CheckBoxHandler : ControlHandler<CheckBox, CheckBoxHandler>, ICheckBoxHandler
    {
        public CheckBoxHandler(CheckBox control) : base(control)
        {
        }

        public bool Checked
        {
            get { return Control.Checked.GetValueOrDefault(); }
            set { Control.Checked = value; }
        }

        public bool Enabled
        {
            get { return Control.Enabled; }
            set { Control.Enabled = value; }
        }

        public string Text
        {
            get { return Control.Text; }
            set { Control.Text = value; }
        }
    }

    public sealed class ComboBoxHandler : ControlHandler<ComboBox, ComboBoxHandler>, IComboBoxHandler
    {
        public ComboBoxHandler(ComboBox control) : base(control)
        {
        }

        public bool Enabled
        {
            get { return Control.Enabled; }
            set {
                Control.Enabled = value;
                //Control.BackgroundColor = (value) ? SystemColors.WindowBackground : SystemColors.Control;
            }
        }

        public bool ReadOnly
        {
            get { return Control.ReadOnly; }
            set { Control.ReadOnly = value; }
        }

        public int SelectedIndex
        {
            get { return Control.SelectedIndex; }
            set { Control.SelectedIndex = value; }
        }

        public object SelectedItem
        {
            get { return Control.SelectedValue; }
            set { Control.SelectedValue = value; }
        }

        public object SelectedTag
        {
            get {
                return ((GKComboItem)Control.SelectedValue).Tag;
            }
            set {
                var ctl = Control;
                foreach (object item in ctl.Items) {
                    GKComboItem comboItem = (GKComboItem)item;
                    if (comboItem.Tag == value) {
                        ctl.SelectedValue = item;
                        return;
                    }
                }
                ctl.SelectedIndex = 0;
            }
        }

        public string Text
        {
            get { return Control.Text; }
            set { Control.Text = value; }
        }

        public void Add(object item)
        {
            Control.Items.Add((string)item);
        }

        public void AddItem(string caption, object tag, IImage image = null)
        {
            Control.Items.Add(new GKComboItem(caption, tag));
        }

        public void AddRange(object[] items, bool sorted = false)
        {
            //Control.Sorted = false;
            Control.Items.AddRange(GKComboItem.Convert((string[])items));
            //Control.Sorted = sorted;
        }

        public void AddStrings(StringList strings)
        {
            int num = strings.Count;
            for (int i = 0; i < num; i++) {
                Control.Items.Add(strings[i]);
            }
        }

        public void BeginUpdate()
        {
            //Control.BeginUpdate();
        }

        public void Clear()
        {
            Control.Items.Clear();
        }

        public void EndUpdate()
        {
            //Control.EndUpdate();
        }

        public void Select()
        {
            Control.Focus();
        }

        public void SortItems()
        {
            Control.SortItems();
        }
    }

    public sealed class TextBoxHandler : ControlHandler<TextBox, TextBoxHandler>, ITextBoxHandler
    {
        public TextBoxHandler(TextBox control) : base(control)
        {
        }

        public bool Enabled
        {
            get { return Control.Enabled; }
            set {
                Control.Enabled = value;
                SetBackColor();
            }
        }

        public string[] Lines
        {
            get { return UIHelper.Convert(Control.Text); }
            set { /* TODO! */ }
        }

        public bool ReadOnly
        {
            get { return Control.ReadOnly; }
            set {
                Control.ReadOnly = value;
                SetBackColor();
            }
        }

        public string Text
        {
            get { return Control.Text; }
            set { Control.Text = value; }
        }

        public void Select()
        {
            Control.Focus();
        }

        private void SetBackColor()
        {
            Control.BackgroundColor = (!Control.ReadOnly && Enabled) ? SystemColors.WindowBackground : SystemColors.Control;
        }
    }

    public sealed class MaskedTextBoxHandler : ControlHandler<MaskedTextBox, MaskedTextBoxHandler>, ITextBoxHandler
    {
        public MaskedTextBoxHandler(MaskedTextBox control) : base(control)
        {
        }

        public bool Enabled
        {
            get { return Control.Enabled; }
            set {
                Control.Enabled = value;
                //Control.BackgroundColor = (value) ? SystemColors.WindowBackground : SystemColors.Control;
            }
        }

        public string[] Lines
        {
            get { return UIHelper.Convert(Control.Text); }
            set { /* TODO! */ }
        }

        public bool ReadOnly
        {
            get { return Control.ReadOnly; }
            set { Control.ReadOnly = value; }
        }

        public string Text
        {
            get { return Control.Text; }
            set { Control.Text = value; }
        }

        public void Select()
        {
            Control.Focus();
        }
    }

    public sealed class NumericBoxHandler : ControlHandler<NumericUpDown, NumericBoxHandler>, INumericBoxHandler
    {
        public NumericBoxHandler(NumericUpDown control) : base(control)
        {
        }

        public bool Enabled
        {
            get { return Control.Enabled; }
            set { Control.Enabled = value; }
        }

        public bool ReadOnly
        {
            get { return Control.ReadOnly; }
            set { Control.ReadOnly = value; }
        }

        public string Text
        {
            get { return Value.ToString(); }
            set { /* TODO */ }
        }

        public int Value
        {
            get { return (int)Control.Value; }
            set { Control.Value = value; }
        }

        public void Select()
        {
            //Control.Select();
        }
    }
}

﻿/*
 *  "GEDKeeper", the personal genealogical database editor.
 *  Copyright (C) 2009-2019 by Sergey V. Zhdanovskih.
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

using System;
using BSLib.Calendar;
using GKCore.Types;

namespace GKCommon.GEDCOM
{
    public sealed class GEDCOMDateRange : GEDCOMCustomDate
    {
        private GEDCOMDate fDateAfter;
        private GEDCOMDate fDateBefore;

        public GEDCOMDate After
        {
            get { return fDateAfter; }
        }

        public GEDCOMDate Before
        {
            get { return fDateBefore; }
        }


        public new static GEDCOMTag Create(GEDCOMTree owner, GEDCOMObject parent, string tagName, string tagValue)
        {
            return new GEDCOMDateRange(owner, parent, tagName, tagValue);
        }

        public GEDCOMDateRange(GEDCOMTree owner, GEDCOMObject parent) : base(owner, parent)
        {
            fDateAfter = new GEDCOMDate(owner, this);
            fDateBefore = new GEDCOMDate(owner, this);
        }

        public GEDCOMDateRange(GEDCOMTree owner, GEDCOMObject parent, string tagName, string tagValue) : this(owner, parent)
        {
            SetNameValue(tagName, tagValue);
        }

        protected override string GetStringValue()
        {
            string result;
            if (!fDateAfter.IsEmpty() && !fDateBefore.IsEmpty()) {
                result = string.Concat(GEDCOMDateRangeArray[2], " ", fDateAfter.StringValue, " ", GEDCOMDateRangeArray[3], " ", fDateBefore.StringValue);
            } else if (!fDateAfter.IsEmpty()) {
                result = GEDCOMDateRangeArray[0] + " " + fDateAfter.StringValue;
            } else if (!fDateBefore.IsEmpty()) {
                result = GEDCOMDateRangeArray[1] + " " + fDateBefore.StringValue;
            } else {
                result = "";
            }
            return result;
        }

        public override DateTime GetDateTime()
        {
            DateTime result;
            if (fDateAfter.IsEmpty()) {
                result = fDateBefore.GetDateTime();
            } else if (fDateBefore.IsEmpty()) {
                result = fDateAfter.GetDateTime();
            } else {
                result = new DateTime(0);
            }
            return result;
        }

        public override void SetDateTime(DateTime value)
        {
            // The risk of undefined behavior
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) {
                fDateAfter.Dispose();
                fDateBefore.Dispose();
            }
            base.Dispose(disposing);
        }

        public override void Clear()
        {
            base.Clear();

            fDateAfter.Clear();
            fDateBefore.Clear();
        }

        public override bool IsEmpty()
        {
            return base.IsEmpty() && fDateAfter.IsEmpty() && fDateBefore.IsEmpty();
        }

        public override string ParseString(string strValue)
        {
            fDateAfter.Clear();
            fDateBefore.Clear();

            string result;
            if (string.IsNullOrEmpty(strValue)) {
                result = string.Empty;
            } else {
                result = GEDCOMUtils.ParseRangeDate(strValue, this);
            }
            return result;
        }

        public override void ResetOwner(GEDCOMTree newOwner)
        {
            base.ResetOwner(newOwner);
            if (fDateAfter != null) fDateAfter.ResetOwner(newOwner);
            if (fDateBefore != null) fDateBefore.ResetOwner(newOwner);
        }

        public override UDN GetUDN()
        {
            UDN result;

            if (fDateAfter.StringValue == "" && fDateBefore.StringValue != "") {
                result = UDN.CreateBefore(fDateBefore.GetUDN());
            } else if (fDateAfter.StringValue != "" && fDateBefore.StringValue == "") {
                result = UDN.CreateAfter(fDateAfter.GetUDN());
            } else if (fDateAfter.StringValue != "" && fDateBefore.StringValue != "") {
                result = UDN.CreateBetween(fDateAfter.GetUDN(), fDateBefore.GetUDN());
            } else {
                result = UDN.CreateEmpty();
            }

            return result;
        }

        public override string GetDisplayStringExt(DateFormat format, bool sign, bool showCalendar)
        {
            string result = "";

            if (fDateAfter.StringValue == "" && fDateBefore.StringValue != "") {
                result = fDateBefore.GetDisplayString(format, true, showCalendar);
                if (sign) result = "< " + result;
            } else if (fDateAfter.StringValue != "" && fDateBefore.StringValue == "") {
                result = fDateAfter.GetDisplayString(format, true, showCalendar);
                if (sign) result += " >";
            } else if (fDateAfter.StringValue != "" && fDateBefore.StringValue != "") {
                result = fDateAfter.GetDisplayString(format, true, showCalendar) + " - " + fDateBefore.GetDisplayString(format, true, showCalendar);
            }

            return result;
        }
    }
}

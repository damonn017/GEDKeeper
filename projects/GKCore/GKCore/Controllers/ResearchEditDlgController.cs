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

using System;
using GKCommon.GEDCOM;
using GKCore.Options;
using GKCore.Types;
using GKCore.UIContracts;

namespace GKCore.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class ResearchEditDlgController : DialogController<IResearchEditDlg>
    {
        private GEDCOMResearchRecord fResearch;

        public GEDCOMResearchRecord Research
        {
            get { return fResearch; }
            set {
                if (fResearch != value) {
                    fResearch = value;
                    UpdateView();
                }
            }
        }


        public ResearchEditDlgController(IResearchEditDlg view) : base(view)
        {
            for (GKResearchPriority rp = GKResearchPriority.rpNone; rp <= GKResearchPriority.rpTop; rp++) {
                fView.Priority.Add(LangMan.LS(GKData.PriorityNames[(int)rp]));
            }

            for (GKResearchStatus rs = GKResearchStatus.rsDefined; rs <= GKResearchStatus.rsWithdrawn; rs++) {
                fView.Status.Add(LangMan.LS(GKData.StatusNames[(int)rs]));
            }
        }

        public override bool Accept()
        {
            try {
                fResearch.ResearchName = fView.Name.Text;
                fResearch.Priority = (GKResearchPriority)fView.Priority.SelectedIndex;
                fResearch.Status = (GKResearchStatus)fView.Status.SelectedIndex;
                fResearch.StartDate.Assign(GEDCOMDate.CreateByFormattedStr(fView.StartDate.Text, true));
                fResearch.StopDate.Assign(GEDCOMDate.CreateByFormattedStr(fView.StopDate.Text, true));
                fResearch.Percent = int.Parse(fView.Percent.Text);

                fLocalUndoman.Commit();

                fBase.NotifyRecord(fResearch, RecordAction.raEdit);

                return true;
            } catch (Exception ex) {
                Logger.LogWrite("ResearchEditDlgController.Accept(): " + ex.Message);
                return false;
            }
        }

        public override void UpdateView()
        {
            if (fResearch == null) {
                fView.Name.Text = "";
                fView.Priority.SelectedIndex = -1;
                fView.Status.SelectedIndex = -1;
                fView.StartDate.Text = "";
                fView.StopDate.Text = "";
                fView.Percent.Value = 0;
            } else {
                fView.Name.Text = fResearch.ResearchName;
                fView.Priority.SelectedIndex = (int)fResearch.Priority;
                fView.Status.SelectedIndex = (int)fResearch.Status;
                fView.StartDate.Text = fResearch.StartDate.GetDisplayString(DateFormat.dfDD_MM_YYYY);
                fView.StopDate.Text = fResearch.StopDate.GetDisplayString(DateFormat.dfDD_MM_YYYY);
                fView.Percent.Value = fResearch.Percent;
            }

            fView.NotesList.ListModel.DataOwner = fResearch;
            fView.TasksList.ListModel.DataOwner = fResearch;
            fView.CommunicationsList.ListModel.DataOwner = fResearch;
            fView.GroupsList.ListModel.DataOwner = fResearch;
        }
    }
}

﻿using System;
using System.Drawing;
using System.Windows.Forms;

namespace GKUI.Dialogs
{
	partial class EventEditDlg
	{
		private System.ComponentModel.IContainer components = null;

		private System.Windows.Forms.Button btnAccept;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.TabControl PageEventData;
		private System.Windows.Forms.TabPage SheetNotes;
		private System.Windows.Forms.TabPage SheetMultimedia;
		private System.Windows.Forms.TabPage SheetSources;
		private System.Windows.Forms.Button btnAddress;
		private System.Windows.Forms.TabPage SheetCommon;
		private System.Windows.Forms.Label Label1;
		private System.Windows.Forms.Label Label2;
		private System.Windows.Forms.Label Label3;
		private System.Windows.Forms.Label Label4;
		private System.Windows.Forms.Label Label5;
		private System.Windows.Forms.Label LabelAttr;
		private System.Windows.Forms.ComboBox EditEventType;
		private System.Windows.Forms.TextBox EditEventName;
		private System.Windows.Forms.TextBox EditEventPlace;
		private System.Windows.Forms.ComboBox EditEventDateType;
		private System.Windows.Forms.MaskedTextBox EditEventDate1;
		private System.Windows.Forms.MaskedTextBox EditEventDate2;
		private System.Windows.Forms.TextBox EditEventCause;
		private System.Windows.Forms.TextBox EditEventOrg;
		private System.Windows.Forms.ComboBox EditAttribute;
		private System.Windows.Forms.Button btnPlaceAdd;
		private System.Windows.Forms.Button btnPlaceDelete;
		private System.Windows.Forms.ComboBox cbDate1Calendar;
		private System.Windows.Forms.ComboBox cbDate2Calendar;
		private System.Windows.Forms.CheckBox btnBC1;
		private System.Windows.Forms.CheckBox btnBC2;

		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				if (components != null) {
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EventEditDlg));
			this.btnAccept = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.PageEventData = new System.Windows.Forms.TabControl();
			this.SheetCommon = new System.Windows.Forms.TabPage();
			this.btnBC2 = new System.Windows.Forms.CheckBox();
			this.btnBC1 = new System.Windows.Forms.CheckBox();
			this.Label1 = new System.Windows.Forms.Label();
			this.Label2 = new System.Windows.Forms.Label();
			this.Label3 = new System.Windows.Forms.Label();
			this.Label4 = new System.Windows.Forms.Label();
			this.Label5 = new System.Windows.Forms.Label();
			this.LabelAttr = new System.Windows.Forms.Label();
			this.btnPlaceAdd = new System.Windows.Forms.Button();
			this.btnPlaceDelete = new System.Windows.Forms.Button();
			this.EditEventType = new System.Windows.Forms.ComboBox();
			this.EditEventName = new System.Windows.Forms.TextBox();
			this.EditEventPlace = new System.Windows.Forms.TextBox();
			this.EditEventDateType = new System.Windows.Forms.ComboBox();
			this.EditEventDate1 = new System.Windows.Forms.MaskedTextBox();
			this.EditEventDate2 = new System.Windows.Forms.MaskedTextBox();
			this.EditEventCause = new System.Windows.Forms.TextBox();
			this.EditEventOrg = new System.Windows.Forms.TextBox();
			this.EditAttribute = new System.Windows.Forms.ComboBox();
			this.cbDate1Calendar = new System.Windows.Forms.ComboBox();
			this.cbDate2Calendar = new System.Windows.Forms.ComboBox();
			this.SheetNotes = new System.Windows.Forms.TabPage();
			this.SheetMultimedia = new System.Windows.Forms.TabPage();
			this.SheetSources = new System.Windows.Forms.TabPage();
			this.btnAddress = new System.Windows.Forms.Button();
			this.PageEventData.SuspendLayout();
			this.SheetCommon.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnAccept
			// 
			this.btnAccept.Image = ((System.Drawing.Image)(resources.GetObject("btnAccept.Image")));
			this.btnAccept.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.btnAccept.Location = new System.Drawing.Point(371, 447);
			this.btnAccept.Name = "btnAccept";
			this.btnAccept.Size = new System.Drawing.Size(113, 30);
			this.btnAccept.TabIndex = 2;
			this.btnAccept.Text = "Принять";
			this.btnAccept.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.btnAccept.Click += new System.EventHandler(this.btnAccept_Click);
			// 
			// btnCancel
			// 
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Image = ((System.Drawing.Image)(resources.GetObject("btnCancel.Image")));
			this.btnCancel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.btnCancel.Location = new System.Drawing.Point(494, 447);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(114, 30);
			this.btnCancel.TabIndex = 3;
			this.btnCancel.Text = "Отменить";
			this.btnCancel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// PageEventData
			// 
			this.PageEventData.Controls.Add(this.SheetCommon);
			this.PageEventData.Controls.Add(this.SheetNotes);
			this.PageEventData.Controls.Add(this.SheetMultimedia);
			this.PageEventData.Controls.Add(this.SheetSources);
			this.PageEventData.Dock = System.Windows.Forms.DockStyle.Top;
			this.PageEventData.Location = new System.Drawing.Point(0, 0);
			this.PageEventData.Name = "PageEventData";
			this.PageEventData.SelectedIndex = 0;
			this.PageEventData.Size = new System.Drawing.Size(620, 429);
			this.PageEventData.TabIndex = 0;
			// 
			// SheetCommon
			// 
			this.SheetCommon.Controls.Add(this.btnBC2);
			this.SheetCommon.Controls.Add(this.btnBC1);
			this.SheetCommon.Controls.Add(this.Label1);
			this.SheetCommon.Controls.Add(this.Label2);
			this.SheetCommon.Controls.Add(this.Label3);
			this.SheetCommon.Controls.Add(this.Label4);
			this.SheetCommon.Controls.Add(this.Label5);
			this.SheetCommon.Controls.Add(this.LabelAttr);
			this.SheetCommon.Controls.Add(this.btnPlaceAdd);
			this.SheetCommon.Controls.Add(this.btnPlaceDelete);
			this.SheetCommon.Controls.Add(this.EditEventType);
			this.SheetCommon.Controls.Add(this.EditEventName);
			this.SheetCommon.Controls.Add(this.EditEventPlace);
			this.SheetCommon.Controls.Add(this.EditEventDateType);
			this.SheetCommon.Controls.Add(this.EditEventDate1);
			this.SheetCommon.Controls.Add(this.EditEventDate2);
			this.SheetCommon.Controls.Add(this.EditEventCause);
			this.SheetCommon.Controls.Add(this.EditEventOrg);
			this.SheetCommon.Controls.Add(this.EditAttribute);
			this.SheetCommon.Controls.Add(this.cbDate1Calendar);
			this.SheetCommon.Controls.Add(this.cbDate2Calendar);
			this.SheetCommon.Location = new System.Drawing.Point(4, 26);
			this.SheetCommon.Name = "SheetCommon";
			this.SheetCommon.Size = new System.Drawing.Size(612, 399);
			this.SheetCommon.TabIndex = 0;
			this.SheetCommon.Text = "Общее";
			// 
			// btnBC2
			// 
			this.btnBC2.AutoSize = true;
			this.btnBC2.Location = new System.Drawing.Point(542, 240);
			this.btnBC2.Name = "btnBC2";
			this.btnBC2.Size = new System.Drawing.Size(47, 21);
			this.btnBC2.TabIndex = 17;
			this.btnBC2.Text = "BC";
			this.btnBC2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.btnBC2.UseVisualStyleBackColor = true;
			// 
			// btnBC1
			// 
			this.btnBC1.AutoSize = true;
			this.btnBC1.Location = new System.Drawing.Point(332, 242);
			this.btnBC1.Name = "btnBC1";
			this.btnBC1.Size = new System.Drawing.Size(47, 21);
			this.btnBC1.TabIndex = 14;
			this.btnBC1.Text = "BC";
			this.btnBC1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.btnBC1.UseVisualStyleBackColor = true;
			// 
			// Label1
			// 
			this.Label1.AutoSize = true;
			this.Label1.Location = new System.Drawing.Point(11, 10);
			this.Label1.Name = "Label1";
			this.Label1.Size = new System.Drawing.Size(66, 17);
			this.Label1.TabIndex = 0;
			this.Label1.Text = "Событие";
			// 
			// Label2
			// 
			this.Label2.AutoSize = true;
			this.Label2.Location = new System.Drawing.Point(11, 126);
			this.Label2.Name = "Label2";
			this.Label2.Size = new System.Drawing.Size(48, 17);
			this.Label2.TabIndex = 5;
			this.Label2.Text = "Место";
			// 
			// Label3
			// 
			this.Label3.AutoSize = true;
			this.Label3.Location = new System.Drawing.Point(11, 185);
			this.Label3.Name = "Label3";
			this.Label3.Size = new System.Drawing.Size(40, 17);
			this.Label3.TabIndex = 10;
			this.Label3.Text = "Дата";
			// 
			// Label4
			// 
			this.Label4.AutoSize = true;
			this.Label4.Location = new System.Drawing.Point(11, 282);
			this.Label4.Name = "Label4";
			this.Label4.Size = new System.Drawing.Size(64, 17);
			this.Label4.TabIndex = 18;
			this.Label4.Text = "Причина";
			// 
			// Label5
			// 
			this.Label5.AutoSize = true;
			this.Label5.Location = new System.Drawing.Point(11, 340);
			this.Label5.Name = "Label5";
			this.Label5.Size = new System.Drawing.Size(207, 17);
			this.Label5.TabIndex = 20;
			this.Label5.Text = "Засвидетельствовавший орган";
			// 
			// LabelAttr
			// 
			this.LabelAttr.AutoSize = true;
			this.LabelAttr.Location = new System.Drawing.Point(11, 68);
			this.LabelAttr.Name = "LabelAttr";
			this.LabelAttr.Size = new System.Drawing.Size(134, 17);
			this.LabelAttr.TabIndex = 3;
			this.LabelAttr.Text = "Значение атрибута";
			// 
			// btnPlaceAdd
			// 
			this.btnPlaceAdd.AccessibleDescription = "Выбрать или добавить место";
			this.btnPlaceAdd.Enabled = false;
			this.btnPlaceAdd.Image = ((System.Drawing.Image)(resources.GetObject("btnPlaceAdd.Image")));
			this.btnPlaceAdd.Location = new System.Drawing.Point(511, 142);
			this.btnPlaceAdd.Name = "btnPlaceAdd";
			this.btnPlaceAdd.Size = new System.Drawing.Size(39, 34);
			this.btnPlaceAdd.TabIndex = 7;
			this.btnPlaceAdd.Click += new System.EventHandler(this.btnPlaceAdd_Click);
			// 
			// btnPlaceDelete
			// 
			this.btnPlaceDelete.AccessibleDescription = "Перейти на запись места";
			this.btnPlaceDelete.Enabled = false;
			this.btnPlaceDelete.Image = ((System.Drawing.Image)(resources.GetObject("btnPlaceDelete.Image")));
			this.btnPlaceDelete.Location = new System.Drawing.Point(557, 142);
			this.btnPlaceDelete.Name = "btnPlaceDelete";
			this.btnPlaceDelete.Size = new System.Drawing.Size(39, 34);
			this.btnPlaceDelete.TabIndex = 9;
			this.btnPlaceDelete.Click += new System.EventHandler(this.btnPlaceDelete_Click);
			// 
			// EditEventType
			// 
			this.EditEventType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.EditEventType.Location = new System.Drawing.Point(11, 29);
			this.EditEventType.Name = "EditEventType";
			this.EditEventType.Size = new System.Drawing.Size(259, 25);
			this.EditEventType.TabIndex = 1;
			this.EditEventType.SelectedIndexChanged += new System.EventHandler(this.EditEventType_SelectedIndexChanged);
			// 
			// EditEventName
			// 
			this.EditEventName.Location = new System.Drawing.Point(280, 29);
			this.EditEventName.Name = "EditEventName";
			this.EditEventName.Size = new System.Drawing.Size(316, 24);
			this.EditEventName.TabIndex = 2;
			// 
			// EditEventPlace
			// 
			this.EditEventPlace.Location = new System.Drawing.Point(11, 146);
			this.EditEventPlace.Name = "EditEventPlace";
			this.EditEventPlace.Size = new System.Drawing.Size(492, 24);
			this.EditEventPlace.TabIndex = 6;
			this.EditEventPlace.KeyDown += new System.Windows.Forms.KeyEventHandler(this.EditEventPlace_KeyDown);
			// 
			// EditEventDateType
			// 
			this.EditEventDateType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.EditEventDateType.Location = new System.Drawing.Point(11, 204);
			this.EditEventDateType.Name = "EditEventDateType";
			this.EditEventDateType.Size = new System.Drawing.Size(168, 25);
			this.EditEventDateType.TabIndex = 11;
			this.EditEventDateType.SelectedIndexChanged += new System.EventHandler(this.EditEventDateType_SelectedIndexChanged);
			// 
			// EditEventDate1
			// 
			this.EditEventDate1.AllowDrop = true;
			this.EditEventDate1.BackColor = System.Drawing.SystemColors.Window;
			this.EditEventDate1.Location = new System.Drawing.Point(190, 204);
			this.EditEventDate1.Mask = "00/00/0000";
			this.EditEventDate1.Name = "EditEventDate1";
			this.EditEventDate1.Size = new System.Drawing.Size(196, 24);
			this.EditEventDate1.TabIndex = 12;
			this.EditEventDate1.TextMaskFormat = System.Windows.Forms.MaskFormat.IncludePromptAndLiterals;
			this.EditEventDate1.DragDrop += new System.Windows.Forms.DragEventHandler(this.EditEventDate1_DragDrop);
			this.EditEventDate1.DragOver += new System.Windows.Forms.DragEventHandler(this.EditEventDate1_DragOver);
			// 
			// EditEventDate2
			// 
			this.EditEventDate2.AllowDrop = true;
			this.EditEventDate2.Location = new System.Drawing.Point(400, 204);
			this.EditEventDate2.Mask = "00/00/0000";
			this.EditEventDate2.Name = "EditEventDate2";
			this.EditEventDate2.Size = new System.Drawing.Size(196, 24);
			this.EditEventDate2.TabIndex = 15;
			this.EditEventDate2.TextMaskFormat = System.Windows.Forms.MaskFormat.IncludePromptAndLiterals;
			this.EditEventDate2.DragDrop += new System.Windows.Forms.DragEventHandler(this.EditEventDate1_DragDrop);
			this.EditEventDate2.DragOver += new System.Windows.Forms.DragEventHandler(this.EditEventDate1_DragOver);
			// 
			// EditEventCause
			// 
			this.EditEventCause.Location = new System.Drawing.Point(11, 301);
			this.EditEventCause.Name = "EditEventCause";
			this.EditEventCause.Size = new System.Drawing.Size(585, 24);
			this.EditEventCause.TabIndex = 19;
			// 
			// EditEventOrg
			// 
			this.EditEventOrg.Location = new System.Drawing.Point(11, 359);
			this.EditEventOrg.Name = "EditEventOrg";
			this.EditEventOrg.Size = new System.Drawing.Size(585, 24);
			this.EditEventOrg.TabIndex = 21;
			// 
			// EditAttribute
			// 
			this.EditAttribute.Location = new System.Drawing.Point(11, 87);
			this.EditAttribute.Name = "EditAttribute";
			this.EditAttribute.Size = new System.Drawing.Size(585, 25);
			this.EditAttribute.TabIndex = 4;
			// 
			// cbDate1Calendar
			// 
			this.cbDate1Calendar.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbDate1Calendar.Location = new System.Drawing.Point(190, 243);
			this.cbDate1Calendar.Name = "cbDate1Calendar";
			this.cbDate1Calendar.Size = new System.Drawing.Size(133, 25);
			this.cbDate1Calendar.TabIndex = 13;
			// 
			// cbDate2Calendar
			// 
			this.cbDate2Calendar.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbDate2Calendar.Location = new System.Drawing.Point(400, 242);
			this.cbDate2Calendar.Name = "cbDate2Calendar";
			this.cbDate2Calendar.Size = new System.Drawing.Size(133, 25);
			this.cbDate2Calendar.TabIndex = 16;
			// 
			// SheetNotes
			// 
			this.SheetNotes.Location = new System.Drawing.Point(4, 26);
			this.SheetNotes.Name = "SheetNotes";
			this.SheetNotes.Size = new System.Drawing.Size(612, 399);
			this.SheetNotes.TabIndex = 1;
			this.SheetNotes.Text = "Заметки";
			// 
			// SheetMultimedia
			// 
			this.SheetMultimedia.Location = new System.Drawing.Point(4, 26);
			this.SheetMultimedia.Name = "SheetMultimedia";
			this.SheetMultimedia.Size = new System.Drawing.Size(612, 399);
			this.SheetMultimedia.TabIndex = 2;
			this.SheetMultimedia.Text = "Мультимедиа";
			// 
			// SheetSources
			// 
			this.SheetSources.Location = new System.Drawing.Point(4, 26);
			this.SheetSources.Name = "SheetSources";
			this.SheetSources.Size = new System.Drawing.Size(612, 399);
			this.SheetSources.TabIndex = 3;
			this.SheetSources.Text = "Источники";
			// 
			// btnAddress
			// 
			this.btnAddress.Location = new System.Drawing.Point(11, 447);
			this.btnAddress.Name = "btnAddress";
			this.btnAddress.Size = new System.Drawing.Size(114, 30);
			this.btnAddress.TabIndex = 1;
			this.btnAddress.Text = "Адрес...";
			this.btnAddress.Click += new System.EventHandler(this.btnAddress_Click);
			// 
			// EventEditDlg
			// 
			this.AcceptButton = this.btnAccept;
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(620, 492);
			this.Controls.Add(this.btnAccept);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.PageEventData);
			this.Controls.Add(this.btnAddress);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "EventEditDlg";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Событие";
			this.PageEventData.ResumeLayout(false);
			this.SheetCommon.ResumeLayout(false);
			this.SheetCommon.PerformLayout();
			this.ResumeLayout(false);
		}
	}
}
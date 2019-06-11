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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BSLib;
using GDModel;
using GDModel.Providers;
using GKCore;

namespace GDModel.Providers.GEDCOM
{
    public enum GEDCOMFormat
    {
        gf_Unknown,
        gf_Native,
        gf_Genealogy_RusOld,

        gf_Ahnenblatt,
        gf_AncestQuest,
        gf_AGES,
        gf_ALTREE,
        gf_EasyTree,
        gf_FamilyHistorian,
        gf_FamilyTreeMaker,
        gf_FTB,
        gf_GeneWeb,
        gf_Geni,
        gf_Genney,
        gf_GenoPro,
        gf_Gramps,
        gf_GENBOX,
        gf_GENJ,
        gf_Heredis,
        gf_Legacy,
        gf_Lifelines,
        gf_PAF,
        gf_Reunion,
        gf_RootsMagic,

        gf_Last = gf_RootsMagic
    }


    public sealed class GEDCOMAppFormat
    {
        public readonly GEDCOMFormat Format;
        public readonly string Sign;
        public readonly string Name;
        public readonly int PredefCharset;

        public GEDCOMAppFormat(GEDCOMFormat format, string sign, string name, int predefCharset)
        {
            Format = format;
            Sign = sign;
            Name = name;
            PredefCharset = predefCharset;
        }
    }


    public class GEDCOMEmptyFileException : GDMException
    {
        public GEDCOMEmptyFileException(string message) : base(message)
        {
        }
    }


    /// <summary>
    /// 
    /// </summary>
    public class GEDCOMProvider : FileProvider
    {
        public const char GEDCOM_DELIMITER = ' ';
        public const char GEDCOM_YEAR_MODIFIER_SEPARATOR = '/';
        public const char GEDCOM_NAME_SEPARATOR = '/';
        public const string GEDCOM_YEAR_BC = "B.C.";
        public const char GEDCOM_POINTER_DELIMITER = '@';
        public const string GEDCOM_NEWLINE = "\r\n";
        public const int MAX_LINE_LENGTH = 248;

        // deprecated
        //public const byte GEDCOMMaxPhoneNumbers = 3;
        //public const byte GEDCOMMaxEmailAddresses = 3;
        //public const byte GEDCOMMaxFaxNumbers = 3;
        //public const byte GEDCOMMaxWebPages = 3;
        //public const byte GEDCOMMaxLanguages = 3;

        public static readonly GEDCOMAppFormat[] GEDCOMFormats;


        public GEDCOMProvider(GDMTree tree) : base(tree)
        {
        }

        public override string GetFilesFilter()
        {
            return LangMan.LS(LSID.LSID_GEDCOMFilter);
        }

        #region Encoding routines

        private enum EncodingState { esUnchecked, esUnchanged, esChanged }

        private const int DEF_CODEPAGE = 437;
        private Encoding fDefaultEncoding;
        private Encoding fSourceEncoding;
        private EncodingState fEncodingState;

        private void SetEncoding(Encoding encoding)
        {
            fSourceEncoding = encoding;
            fEncodingState = (fDefaultEncoding.Equals(fSourceEncoding)) ? EncodingState.esUnchanged : EncodingState.esChanged;
        }

        private void DefineEncoding(StreamReader reader, GEDCOMFormat format, string streamCharset)
        {
            GEDCOMCharacterSet charSet = fTree.Header.CharacterSet.Value;
            switch (charSet)
            {
                case GEDCOMCharacterSet.csUTF8:
                    if (!SysUtils.IsUnicodeEncoding(reader.CurrentEncoding)) {
                        SetEncoding(Encoding.UTF8); // file without BOM
                    } else {
                        fEncodingState = EncodingState.esUnchanged;
                    }
                    break;

                case GEDCOMCharacterSet.csUNICODE:
                    if (format == GEDCOMFormat.gf_Geni) {
                        SetEncoding(Encoding.UTF8);
                    } else if (format == GEDCOMFormat.gf_GENJ) {
                        SetEncoding(Encoding.UTF8);
                    } else {
                        if (!SysUtils.IsUnicodeEncoding(reader.CurrentEncoding)) {
                            SetEncoding(Encoding.Unicode); // file without BOM
                        } else {
                            fEncodingState = EncodingState.esUnchanged;
                        }
                    }
                    break;

                case GEDCOMCharacterSet.csANSEL:
                    if (format == GEDCOMFormat.gf_ALTREE) {
                        // Agelong Tree 4.0 with ANSEL is actually characteristic 
                        // for the Russian-language data export
                        SetEncoding(Encoding.GetEncoding(1251));
                    } else if (format == GEDCOMFormat.gf_Geni) {
                        SetEncoding(Encoding.UTF8);
                    } else {
                        SetEncoding(new AnselEncoding());
                    }
                    break;

                case GEDCOMCharacterSet.csASCII:
                    if (format == GEDCOMFormat.gf_Native) {
                        // GEDKeeper native format (old) and ASCII charset
                        SetEncoding(Encoding.GetEncoding(1251));
                    } else {
                        var fmtProps = GetGEDCOMFormatProps(format);
                        if (fmtProps.PredefCharset > -1) {
                            SetEncoding(Encoding.GetEncoding(fmtProps.PredefCharset));
                        } else {
                            string cpVers = fTree.Header.CharacterSet.Version;
                            if (!string.IsNullOrEmpty(cpVers)) {
                                int sourceCodepage = ConvertHelper.ParseInt(cpVers, DEF_CODEPAGE);
                                SetEncoding(Encoding.GetEncoding(sourceCodepage));
                            } else {
                                if (fTree.Header.Language == GDMLanguageID.Russian) {
                                    SetEncoding(Encoding.GetEncoding(1251));
                                } else {
                                    if (streamCharset == null) {
                                        SetEncoding(Encoding.GetEncoding(DEF_CODEPAGE));
                                    } else {
                                        SetEncoding(Encoding.GetEncoding(streamCharset));
                                    }
                                }
                            }
                        }
                    }
                    break;
            }
        }

        protected override Encoding GetDefaultEncoding()
        {
            return Encoding.GetEncoding(DEF_CODEPAGE);
        }

        protected override string DetectCharset(Stream inputStream, bool charsetDetection)
        {
            string streamCharset = null;
            if (charsetDetection) {
                var charsetRes = GKUtils.DetectCharset(inputStream);
                if (charsetRes.Confidence >= 0.7f) {
                    streamCharset = charsetRes.Charset;
                }
            }
            return streamCharset;
        }

        #endregion

        #region Buffered read without excessive allocating memory

        private const int SB_SIZE = 32 * 1024;
        private const int LB_SIZE = 1024;

        private char[] fStreamBuffer, fLineBuffer;
        private int fStmBufLen, fStmBufPos, fLineBufPos;

        private void InitBuffers()
        {
            fStreamBuffer = new char[SB_SIZE];
            fLineBuffer = new char[LB_SIZE];
            fStmBufLen = 0;
            fStmBufPos = 0;
            fLineBufPos = 0;
        }

        private int ReadLine(StreamReader reader)
        {
            while (true) {
                if (fStmBufPos >= fStmBufLen) {
                    fStmBufLen = reader.Read(fStreamBuffer, 0, SB_SIZE);
                    if (fStmBufLen <= 0 && fLineBufPos <= 0) {
                        return -1; // eof, no more lines and no line's buffer
                    }
                    fStmBufPos = 0;
                }

                // here '\r' - it's replace for \0, to reduce checks
                char ch = (fStmBufPos >= fStmBufLen) ? '\r' : fStreamBuffer[fStmBufPos];
                fStmBufPos += 1;

                if (ch == '\r' || ch == '\n') {
                    int linePos = fLineBufPos;
                    fLineBufPos = 0;
                    if (linePos > 0) {
                        if (fEncodingState == EncodingState.esChanged) {
                            byte[] src = fDefaultEncoding.GetBytes(fLineBuffer, 0, linePos);
                            linePos = fSourceEncoding.GetChars(src, 0, src.Length, fLineBuffer, 0);
                        }

                        return linePos;
                    }
                } else {
                    fLineBuffer[fLineBufPos] = ch;
                    fLineBufPos += 1;
                }
            }
        }

        #endregion

        #region Loading functions

        protected override void LoadFromReader(Stream fileStream, StreamReader reader, string streamCharset = null)
        {
            fTree.State = GDMTreeState.osLoading;
            try {
                ProgressEventHandler progressHandler = fTree.OnProgress;

                fDefaultEncoding = GetDefaultEncoding();
                fSourceEncoding = fDefaultEncoding;
                fEncodingState = EncodingState.esUnchecked;

                long fileSize = fileStream.Length;
                int progress = 0;
                var invariantText = GEDCOMUtils.InvariantTextInfo;

                InitBuffers();
                var strTok = new GEDCOMParser(false);
                GDMTag curRecord = null;
                GDMTag curTag = null;
                var stack = new Stack<StackTuple>(9);

                int lineNum = 0;
                int lineLen;
                while ((lineLen = ReadLine(reader)) != -1) {
                    lineNum++;

                    int tagLevel;
                    string tagXRef, tagName, tagValue;

                    try {
                        strTok.Reset(fLineBuffer, 0, lineLen);
                        int lineRes = GEDCOMUtils.ParseTag(strTok, out tagLevel, out tagXRef, out tagName, out tagValue);

                        // empty line
                        if (lineRes == -2) continue;

                        // line with text but not in standard tag format
                        if (lineRes == -1) {
                            if (fTree.Format == GEDCOMFormat.gf_FTB) {
                                FixFTBLine(curRecord, curTag, lineNum, tagValue);
                                continue;
                            } else {
                                throw new GDMException(string.Format("The string {0} doesn't start with a valid number", lineNum));
                            }
                        }

                        tagName = invariantText.ToUpper(tagName);
                    } catch (GDMException ex) {
                        throw new GDMException("Syntax error in line " + Convert.ToString(lineNum) + ".\r" + ex.Message);
                    }

                    if (tagLevel == 0) {
                        if (curRecord == fTree.Header && fEncodingState == EncodingState.esUnchecked) {
                            // beginning recognition of the first is not header record
                            // to check for additional versions of the code page
                            var format = GetGEDCOMFormat(fTree);
                            fTree.Format = format;
                            DefineEncoding(reader, format, streamCharset);
                        }

                        StackTuple stackTuple = AddTreeTag(fTree, tagLevel, tagName, tagValue);
                        if (stackTuple != null) {
                            stack.Clear();
                            stack.Push(stackTuple);

                            curRecord = stackTuple.Tag;
                            if (!string.IsNullOrEmpty(tagXRef)) {
                                ((GDMRecord)curRecord).XRef = tagXRef;
                            }
                            curTag = null;
                        } else {
                            // only TRLR
                            break;
                        }
                    } else {
                        if (curRecord != null) {
                            curTag = ProcessTag(stack, tagLevel, tagName, tagValue);
                        }
                    }

                    if (progressHandler != null) {
                        int newProgress = (int)Math.Min(100, (fileStream.Position * 100.0f) / fileSize);
                        if (progress != newProgress) {
                            progress = newProgress;
                            progressHandler(fTree, progress);
                        }
                    }
                }

                stack.Clear();

                if (lineNum == 0) {
                    throw new GEDCOMEmptyFileException("GEDCOM file is empty");
                }
            } finally {
                fTree.State = GDMTreeState.osReady;
            }
        }

        internal static GDMTag ProcessTag(Stack<StackTuple> stack, int tagLevel, string tagName, string tagValue)
        {
            GDMTag curTag = null;

            GDMTag parentTag = null;
            AddTagHandler addTagHandler = null;
            while (stack.Count > 0) {
                var tuple = stack.Peek();
                if (tagLevel > tuple.Level) {
                    parentTag = tuple.Tag;
                    addTagHandler = tuple.AddHandler;
                    break;
                }
                stack.Pop();
            }

            if (parentTag != null) {
                if (addTagHandler != null) {
                    var tuple = addTagHandler(parentTag, tagLevel, tagName, tagValue);
                    if (tuple != null) {
                        stack.Push(tuple);
                        curTag = tuple.Tag;
                    }
                } else {
                    curTag = parentTag.AddTag(tagName, tagValue, null);
                    if (curTag != null) {
                        stack.Push(new StackTuple(tagLevel, curTag));
                    }
                }
            }

            return curTag;
        }

        internal static StackTuple AddTreeTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMTree tree = (GDMTree)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.INDI) {
                curTag = tree.AddRecord(new GDMIndividualRecord(tree));
                addHandler = AddIndividualRecordTag;

            } else if (tagName == GEDCOMTagType.FAM) {
                curTag = tree.AddRecord(new GDMFamilyRecord(tree));
                addHandler = AddFamilyRecordTag;

            } else if (tagName == GEDCOMTagType.OBJE) {
                curTag = tree.AddRecord(new GDMMultimediaRecord(tree));
                addHandler = AddMultimediaRecordTag;

            } else if (tagName == GEDCOMTagType.NOTE) {
                curTag = tree.AddRecord(new GDMNoteRecord(tree));
                curTag.ParseString(tagValue);
                addHandler = AddNoteRecordTag;

            } else if (tagName == GEDCOMTagType.REPO) {
                curTag = tree.AddRecord(new GDMRepositoryRecord(tree));
                addHandler = AddRepositoryRecordTag;

            } else if (tagName == GEDCOMTagType.SOUR) {
                curTag = tree.AddRecord(new GDMSourceRecord(tree));
                addHandler = AddSourceRecordTag;

            } else if (tagName == GEDCOMTagType.SUBN) {
                curTag = tree.AddRecord(new GDMSubmissionRecord(tree));
                addHandler = AddSubmissionRecordTag;

            } else if (tagName == GEDCOMTagType.SUBM) {
                curTag = tree.AddRecord(new GDMSubmitterRecord(tree));
                addHandler = AddSubmitterRecordTag;

            } else if (tagName == GEDCOMTagType._GROUP) {
                curTag = tree.AddRecord(new GDMGroupRecord(tree));
                addHandler = AddGroupRecordTag;

            } else if (tagName == GEDCOMTagType._RESEARCH) {
                curTag = tree.AddRecord(new GDMResearchRecord(tree));
                addHandler = AddResearchRecordTag;

            } else if (tagName == GEDCOMTagType._TASK) {
                curTag = tree.AddRecord(new GDMTaskRecord(tree));
                addHandler = AddTaskRecordTag;

            } else if (tagName == GEDCOMTagType._COMM) {
                curTag = tree.AddRecord(new GDMCommunicationRecord(tree));
                addHandler = AddCommunicationRecordTag;

            } else if (tagName == GEDCOMTagType._LOC) {
                curTag = tree.AddRecord(new GDMLocationRecord(tree));
                addHandler = AddLocationRecordTag;

            } else if ((tagName == GEDCOMTagType._PLAC) && (tree.Format == GEDCOMFormat.gf_FamilyHistorian)) {
                curTag = tree.AddRecord(new GDMLocationRecord(tree));
                ((GDMLocationRecord)curTag).LocationName = tagValue;
                addHandler = AddLocationRecordTag;

            } else if (tagName == GEDCOMTagType.HEAD) {
                curTag = tree.Header;
                addHandler = AddHeaderTag;

            } else if (tagName == GEDCOMTagType.TRLR) {
                curTag = null;

            } else {
                curTag = tree.AddRecord(new GDMRecord(tree));
                addHandler = AddRecordTag;
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        #endregion

        #region Saving functions

        public void SaveToFile(string fileName, GEDCOMCharacterSet charSet)
        {
            // Attention: processing of Header moved to BaseContext!

            using (FileStream fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write)) {
                SaveToStreamExt(fileStream, charSet);
            }
        }

        public void SaveToStreamExt(Stream outputStream, GEDCOMCharacterSet charSet)
        {
            // Attention: processing of Header moved to BaseContext!

            StreamWriter writer = new StreamWriter(outputStream, GEDCOMUtils.GetEncodingByCharacterSet(charSet));
            IList<GDMRecord> records = fTree.GetRecords().GetList();
            SaveToStream(writer, records);
            writer.Flush();
        }

        public void SaveToStream(StreamWriter writer, IList<GDMRecord> list)
        {
            // write header
            WriteHeader(writer, 0, fTree.Header);

            if (list != null) {
                int num = list.Count;
                for (int i = 0; i < num; i++) {
                    GDMRecord record = list[i];
                    WriteRecordEx(writer, record);
                }
            }

            // write footer
            WriteTagLine(writer, 0, GEDCOMTagType.TRLR, string.Empty);
        }

        public static void WriteRecordEx(StreamWriter writer, GDMRecord record)
        {
            switch (record.RecordType) {
                case GDMRecordType.rtIndividual:
                    WriteIndividualRecord(writer, 0, record);
                    break;

                case GDMRecordType.rtFamily:
                    WriteFamilyRecord(writer, 0, record);
                    break;

                case GDMRecordType.rtNote:
                    WriteNoteRecord(writer, 0, record);
                    break;

                case GDMRecordType.rtMultimedia:
                    WriteMultimediaRecord(writer, 0, record);
                    break;

                case GDMRecordType.rtSource:
                    WriteSourceRecord(writer, 0, record);
                    break;

                case GDMRecordType.rtRepository:
                    WriteRepositoryRecord(writer, 0, record);
                    break;

                case GDMRecordType.rtGroup:
                    WriteGroupRecord(writer, 0, record);
                    break;

                case GDMRecordType.rtResearch:
                    WriteResearchRecord(writer, 0, record);
                    break;

                case GDMRecordType.rtTask:
                    WriteTaskRecord(writer, 0, record);
                    break;

                case GDMRecordType.rtCommunication:
                    WriteCommunicationRecord(writer, 0, record);
                    break;

                case GDMRecordType.rtLocation:
                    WriteLocationRecord(writer, 0, record);
                    break;

                case GDMRecordType.rtSubmission:
                    WriteSubmissionRecord(writer, 0, record);
                    break;

                case GDMRecordType.rtSubmitter:
                    WriteSubmitterRecord(writer, 0, record);
                    break;

                default:
                    WriteRecord(writer, 0, record);
                    break;
            }
        }

        #endregion

        #region Unified read/write functions

        private static StackTuple AddIndividualRecordTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMIndividualRecord indiRec = (GDMIndividualRecord)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.FAMC) {
                curTag = indiRec.ChildToFamilyLinks.Add(new GDMChildToFamilyLink(indiRec, tagName, tagValue));
                addHandler = AddChildToFamilyLinkTag;
            } else if (tagName == GEDCOMTagType.FAMS) {
                curTag = indiRec.SpouseToFamilyLinks.Add(new GDMSpouseToFamilyLink(indiRec, tagName, tagValue));
            } else if (tagName == GEDCOMTagType.NAME) {
                curTag = indiRec.AddPersonalName(new GDMPersonalName(indiRec, tagName, tagValue));
                addHandler = AddPersonalNameTag;
            } else if (tagName == GEDCOMTagType.ASSO) {
                curTag = indiRec.Associations.Add(new GDMAssociation(indiRec, tagName, tagValue));
                addHandler = AddAssociationTag;
            } else if (tagName == GEDCOMTagType.ALIA) {
                curTag = indiRec.Aliases.Add(new GDMAlias(indiRec, tagName, tagValue));
            } else if (GEDCOMUtils.IsIndiEvent(tagName)) {
                curTag = indiRec.AddEvent(new GDMIndividualEvent(indiRec, tagName, tagValue));
                addHandler = AddCustomEventTag;
            } else if (GEDCOMUtils.IsIndiAttr(tagName)) {
                curTag = indiRec.AddEvent(new GDMIndividualAttribute(indiRec, tagName, tagValue));
                addHandler = AddCustomEventTag;
            } else if (tagName == GEDCOMTagType._GROUP) {
                curTag = indiRec.Groups.Add(new GDMPointer(indiRec, tagName, tagValue));
            } else if (tagName == GEDCOMTagType.SEX) {
                indiRec.Sex = GEDCOMUtils.GetSexVal(tagValue);
                curTag = null;
            } else if (tagName == GEDCOMTagType.AFN) {
                indiRec.AncestralFileNumber = tagValue;
                curTag = null;
            } else if (tagName == GEDCOMTagType.RFN) {
                indiRec.PermanentRecordFileNumber = tagValue;
                curTag = null;
            } else {
                return AddRecordWithEventsTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static void WriteIndividualRecord(StreamWriter stream, int level, GDMTag tag)
        {
            GDMIndividualRecord indiRec = (GDMIndividualRecord)tag;

            WriteRecordWithEvents(stream, level, indiRec);

            level += 1;
            GEDCOMProvider.WriteTagLine(stream, level, GEDCOMTagType.SEX, GEDCOMUtils.GetSexStr(indiRec.Sex), true);
            GEDCOMProvider.WriteTagLine(stream, level, GEDCOMTagType.AFN, indiRec.AncestralFileNumber, true);
            GEDCOMProvider.WriteTagLine(stream, level, GEDCOMTagType.RFN, indiRec.PermanentRecordFileNumber, true);

            WriteList(stream, level, indiRec.PersonalNames, WritePersonalName);
            WriteList(stream, level, indiRec.ChildToFamilyLinks, WriteChildToFamilyLink);
            WriteList(stream, level, indiRec.SpouseToFamilyLinks, WriteTagEx);
            WriteList(stream, level, indiRec.Events, WriteCustomEvent);
            WriteList(stream, level, indiRec.Associations, WriteAssociation);
            WriteList(stream, level, indiRec.Aliases, WriteTagEx);
            WriteList(stream, level, indiRec.Groups, WriteTagEx);
        }


        private static StackTuple AddFamilyRecordTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMFamilyRecord famRec = (GDMFamilyRecord)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.HUSB) {
                curTag = famRec.Husband;
                curTag.ParseString(tagValue);
            } else if (tagName == GEDCOMTagType.WIFE) {
                curTag = famRec.Wife;
                curTag.ParseString(tagValue);
            } else if (tagName == GEDCOMTagType.CHIL) {
                curTag = famRec.Children.Add(new GDMPointer(famRec, tagName, tagValue));
            } else if (tagName == GEDCOMTagType._STAT) {
                famRec.Status = GEDCOMUtils.GetMarriageStatusVal(tagValue);
                curTag = null;
            } else if (GEDCOMUtils.IsFamEvent(tagName)) {
                curTag = famRec.AddEvent(new GDMFamilyEvent(famRec, tagName, tagValue));
                addHandler = AddCustomEventTag;
            } else {
                return AddRecordWithEventsTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static void WriteFamilyRecord(StreamWriter stream, int level, GDMTag tag)
        {
            GDMFamilyRecord famRec = (GDMFamilyRecord)tag;

            WriteRecordWithEvents(stream, level, famRec);

            level += 1;
            WriteBaseTag(stream, level, famRec.Husband);
            WriteBaseTag(stream, level, famRec.Wife);
            WriteTagLine(stream, level, GEDCOMTagType._STAT, GEDCOMUtils.GetMarriageStatusStr(famRec.Status), true);

            WriteList(stream, level, famRec.Children, WriteTagEx);
            WriteList(stream, level, famRec.Events, WriteCustomEvent);
        }


        private static StackTuple AddRecordWithEventsTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMRecordWithEvents evtRec = (GDMRecordWithEvents)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.RESN) {
                evtRec.Restriction = GEDCOMUtils.GetRestrictionVal(tagValue);
                curTag = null;
            } else if (tagName == GEDCOMTagType.SUBM) {
                curTag = evtRec.Submittors.Add(new GDMPointer(evtRec, tagName, tagValue));
            } else {
                return AddRecordTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static void WriteRecordWithEvents(StreamWriter stream, int level, GDMTag tag)
        {
            GDMRecordWithEvents recWE = (GDMRecordWithEvents)tag;

            WriteRecord(stream, level, recWE);

            level += 1;
            WriteTagLine(stream, level, GEDCOMTagType.RESN, GEDCOMUtils.GetRestrictionStr(recWE.Restriction), true);
            WriteList(stream, level, recWE.Submittors, WriteTagEx);
        }


        private static StackTuple AddGroupRecordTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMGroupRecord groupRec = (GDMGroupRecord)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.NAME) {
                groupRec.GroupName = tagValue;
                curTag = null;
            } else if (tagName == GEDCOMTagType._MEMBER) {
                curTag = groupRec.Members.Add(new GDMPointer(groupRec, tagName, tagValue));
            } else {
                return AddRecordTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static void WriteGroupRecord(StreamWriter stream, int level, GDMTag tag)
        {
            GDMGroupRecord groupRec = (GDMGroupRecord)tag;

            WriteRecord(stream, level, groupRec);

            level += 1;
            WriteTagLine(stream, level, GEDCOMTagType.NAME, groupRec.GroupName, true);
            WriteList(stream, level, groupRec.Members, WriteTagEx);
        }


        private static StackTuple AddMultimediaRecordTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMMultimediaRecord mmRec = (GDMMultimediaRecord)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.FILE) {
                curTag = mmRec.FileReferences.Add(new GDMFileReferenceWithTitle(mmRec, tagName, tagValue));
                addHandler = AddFileReferenceWithTitleTag;
            } else {
                return AddRecordTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static void WriteMultimediaRecord(StreamWriter stream, int level, GDMTag tag)
        {
            GDMMultimediaRecord mmRec = (GDMMultimediaRecord)tag;

            WriteRecord(stream, level, mmRec);
            WriteList(stream, ++level, mmRec.FileReferences, WriteFileReferenceWithTitle);
        }


        private static StackTuple AddSourceRecordTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMSourceRecord sourRec = (GDMSourceRecord)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.REPO) {
                curTag = sourRec.RepositoryCitations.Add(new GDMRepositoryCitation(sourRec, tagName, tagValue));
            } else if (tagName == GEDCOMTagType.DATA) {
                curTag = sourRec.Data;
                addHandler = AddSourceDataTag;
            } else if (tagName == GEDCOMTagType.AUTH) {
                curTag = sourRec.Originator;
                curTag.ParseString(tagValue);
                addHandler = AddTextTag;
            } else if (tagName == GEDCOMTagType.PUBL) {
                curTag = sourRec.Publication;
                curTag.ParseString(tagValue);
                addHandler = AddTextTag;
            } else if (tagName == GEDCOMTagType.ABBR) {
                sourRec.ShortTitle = tagValue;
            } else if (tagName == GEDCOMTagType.TEXT) {
                curTag = sourRec.Text;
                curTag.ParseString(tagValue);
                addHandler = AddTextTag;
            } else if (tagName == GEDCOMTagType.TITL) {
                curTag = sourRec.Title;
                curTag.ParseString(tagValue);
                addHandler = AddTextTag;
            } else {
                return AddRecordTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static void WriteSourceRecord(StreamWriter stream, int level, GDMTag tag)
        {
            GDMSourceRecord sourRec = (GDMSourceRecord)tag;

            WriteRecord(stream, level, sourRec);

            level += 1;
            WriteText(stream, level, sourRec.Title);
            WriteText(stream, level, sourRec.Publication);
            WriteTagLine(stream, level, GEDCOMTagType.ABBR, sourRec.ShortTitle, true);
            WriteList(stream, level, sourRec.RepositoryCitations, WriteTagEx);

            WriteSourceData(stream, level, sourRec.Data);
            WriteText(stream, level, sourRec.Originator);
            WriteText(stream, level, sourRec.Text);
        }


        private static StackTuple AddResearchRecordTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMResearchRecord resRec = (GDMResearchRecord)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.NAME) {
                resRec.ResearchName = tagValue;
            } else if (tagName == GEDCOMTagType._PRIORITY) {
                resRec.Priority = GEDCOMUtils.GetPriorityVal(tagValue);
            } else if (tagName == GEDCOMTagType._STATUS) {
                resRec.Status = GEDCOMUtils.GetStatusVal(tagValue);
            } else if (tagName == GEDCOMTagType._PERCENT) {
                resRec.Percent = GEDCOMUtils.GetIntVal(tagValue);
            } else if (tagName == GEDCOMTagType._STARTDATE) {
                curTag = resRec.StartDate;
                curTag.ParseString(tagValue);
            } else if (tagName == GEDCOMTagType._STOPDATE) {
                curTag = resRec.StopDate;
                curTag.ParseString(tagValue);
            } else if (tagName == GEDCOMTagType._TASK) {
                curTag = resRec.Tasks.Add(new GDMPointer(resRec, tagName, tagValue));
            } else if (tagName == GEDCOMTagType._COMM) {
                curTag = resRec.Communications.Add(new GDMPointer(resRec, tagName, tagValue));
            } else if (tagName == GEDCOMTagType._GROUP) {
                curTag = resRec.Groups.Add(new GDMPointer(resRec, tagName, tagValue));
            } else {
                return AddRecordTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static void WriteResearchRecord(StreamWriter stream, int level, GDMTag tag)
        {
            GDMResearchRecord resRec = (GDMResearchRecord)tag;

            WriteRecord(stream, level, resRec);

            level += 1;

            WriteBaseTag(stream, level, resRec.StartDate);
            WriteBaseTag(stream, level, resRec.StopDate);
            WriteTagLine(stream, level, GEDCOMTagType.NAME, resRec.ResearchName, true);
            WriteTagLine(stream, level, GEDCOMTagType._PRIORITY, GEDCOMUtils.GetPriorityStr(resRec.Priority), true);
            WriteTagLine(stream, level, GEDCOMTagType._STATUS, GEDCOMUtils.GetStatusStr(resRec.Status), true);
            WriteTagLine(stream, level, GEDCOMTagType._PERCENT, GEDCOMUtils.GetIntStr(resRec.Percent), true);

            WriteList(stream, level, resRec.Tasks, WriteTagEx);
            WriteList(stream, level, resRec.Communications, WriteTagEx);
            WriteList(stream, level, resRec.Groups, WriteTagEx);
        }


        private static StackTuple AddNoteRecordTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            /*GDMNoteRecord noteRec = (GDMNoteRecord)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;*/

            return AddRecordTag(owner, tagLevel, tagName, tagValue);

            /*return CreateReaderStackTuple(tagLevel, curTag, addHandler);*/
        }

        private static void WriteNoteRecord(StreamWriter stream, int level, GDMTag tag)
        {
            GDMNoteRecord noteRec = (GDMNoteRecord)tag;

            WriteRecordValue(stream, level, noteRec);
            level += 1;
            WriteSubTags(stream, level, tag);
            if (!DebugWrite) {
                WriteTagLine(stream, level, GEDCOMTagType._UID, noteRec.UID, true);
                WriteChangeDate(stream, level, noteRec.ChangeDate);
            }
            WriteList(stream, level, noteRec.SourceCitations, WriteSourceCitation);
            WriteList(stream, level, noteRec.UserReferences, WriteUserReference);
        }


        private static StackTuple AddRepositoryRecordTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMRepositoryRecord repoRec = (GDMRepositoryRecord)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.NAME) {
                repoRec.RepositoryName = tagValue;
                curTag = null;
            } else if (tagName == GEDCOMTagType.ADDR) {
                curTag = repoRec.Address;
                curTag.ParseString(tagValue);
                addHandler = AddAddressTag;
            } else if (tagName == GEDCOMTagType.PHON || tagName == GEDCOMTagType.EMAIL || tagName == GEDCOMTagType.FAX || tagName == GEDCOMTagType.WWW) {
                return AddAddressTag(repoRec.Address, tagLevel, tagName, tagValue);
            } else {
                return AddRecordTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static void WriteRepositoryRecord(StreamWriter stream, int level, GDMTag tag)
        {
            GDMRepositoryRecord repoRec = (GDMRepositoryRecord)tag;

            WriteRecord(stream, level, repoRec);

            level += 1;
            WriteTagLine(stream, level, GEDCOMTagType.NAME, repoRec.RepositoryName, true);
            WriteAddress(stream, level, repoRec.Address);
        }


        private static StackTuple AddTaskRecordTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMTaskRecord taskRec = (GDMTaskRecord)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType._GOAL) {
                taskRec.Goal = tagValue;
            } else if (tagName == GEDCOMTagType._PRIORITY) {
                taskRec.Priority = GEDCOMUtils.GetPriorityVal(tagValue);
            } else if (tagName == GEDCOMTagType._STARTDATE) {
                curTag = taskRec.StartDate;
                curTag.ParseString(tagValue);
            } else if (tagName == GEDCOMTagType._STOPDATE) {
                curTag = taskRec.StopDate;
                curTag.ParseString(tagValue);
            } else {
                return AddRecordTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static void WriteTaskRecord(StreamWriter stream, int level, GDMTag tag)
        {
            GDMTaskRecord taskRec = (GDMTaskRecord)tag;

            WriteRecord(stream, level, taskRec);

            level += 1;
            WriteBaseTag(stream, level, taskRec.StartDate);
            WriteBaseTag(stream, level, taskRec.StopDate);
            WriteTagLine(stream, level, GEDCOMTagType._PRIORITY, GEDCOMUtils.GetPriorityStr(taskRec.Priority), true);
            WriteTagLine(stream, level, GEDCOMTagType._GOAL, taskRec.Goal, true);
        }


        private static StackTuple AddCommunicationRecordTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMCommunicationRecord commRec = (GDMCommunicationRecord)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.NAME) {
                commRec.CommName = tagValue;
                curTag = null;
            } else if (tagName == GEDCOMTagType.TYPE) {
                commRec.CommunicationType = GEDCOMUtils.GetCommunicationTypeVal(tagValue);
                curTag = null;
            } else if (tagName == GEDCOMTagType.FROM) {
                commRec.CommDirection = GDMCommunicationDir.cdFrom;
                commRec.Corresponder.ParseString(tagValue);
                curTag = null;
            } else if (tagName == GEDCOMTagType.TO) {
                commRec.CommDirection = GDMCommunicationDir.cdTo;
                commRec.Corresponder.ParseString(tagValue);
                curTag = null;
            } else if (tagName == GEDCOMTagType.DATE) {
                curTag = commRec.Date;
                curTag.ParseString(tagValue);
            } else {
                return AddRecordTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static void WriteCommunicationRecord(StreamWriter stream, int level, GDMTag tag)
        {
            GDMCommunicationRecord commRec = (GDMCommunicationRecord)tag;

            WriteRecord(stream, level, commRec);

            level += 1;
            WriteBaseTag(stream, level, commRec.Date);
            WriteTagLine(stream, level, GEDCOMTagType.NAME, commRec.CommName, true);
            WriteTagLine(stream, level, GEDCOMTagType.TYPE, GEDCOMUtils.GetCommunicationTypeStr(commRec.CommunicationType), true);
            WriteBaseTag(stream, level, commRec.Corresponder);
        }


        private static StackTuple AddSubmissionRecordTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMSubmissionRecord submnRec = (GDMSubmissionRecord)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.SUBM) {
                curTag = submnRec.Submitter;
                curTag.ParseString(tagValue);
            } else if (tagName == GEDCOMTagType.FAMF) {
                submnRec.FamilyFileName = tagValue;
            } else if (tagName == GEDCOMTagType.TEMP) {
                submnRec.TempleCode = tagValue;
            } else if (tagName == GEDCOMTagType.ANCE) {
                submnRec.GenerationsOfAncestors = GEDCOMUtils.GetIntVal(tagValue);
            } else if (tagName == GEDCOMTagType.DESC) {
                submnRec.GenerationsOfDescendants = GEDCOMUtils.GetIntVal(tagValue);
            } else if (tagName == GEDCOMTagType.ORDI) {
                submnRec.OrdinanceProcessFlag = GEDCOMUtils.GetOrdinanceProcessFlagVal(tagValue);
            } else {
                return AddRecordTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static void WriteSubmissionRecord(StreamWriter stream, int level, GDMTag tag)
        {
            GDMSubmissionRecord submnRec = (GDMSubmissionRecord)tag;

            WriteRecord(stream, level, submnRec);

            level += 1;
            WriteBaseTag(stream, level, submnRec.Submitter);
            WriteTagLine(stream, level, GEDCOMTagType.FAMF, submnRec.FamilyFileName, true);
            WriteTagLine(stream, level, GEDCOMTagType.TEMP, submnRec.TempleCode, true);
            WriteTagLine(stream, level, GEDCOMTagType.ANCE, GEDCOMUtils.GetIntStr(submnRec.GenerationsOfAncestors), true);
            WriteTagLine(stream, level, GEDCOMTagType.DESC, GEDCOMUtils.GetIntStr(submnRec.GenerationsOfDescendants), true);
            WriteTagLine(stream, level, GEDCOMTagType.ORDI, GEDCOMUtils.GetOrdinanceProcessFlagStr(submnRec.OrdinanceProcessFlag), true);
        }


        private static StackTuple AddSubmitterRecordTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMSubmitterRecord submrRec = (GDMSubmitterRecord)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.NAME) {
                curTag = submrRec.Name;
                curTag.ParseString(tagValue);
                addHandler = AddPersonalNameTag;
            } else if (tagName == GEDCOMTagType.ADDR) {
                curTag = submrRec.Address;
                curTag.ParseString(tagValue);
                addHandler = AddAddressTag;
            } else if (tagName == GEDCOMTagType.PHON || tagName == GEDCOMTagType.EMAIL || tagName == GEDCOMTagType.FAX || tagName == GEDCOMTagType.WWW) {
                return AddAddressTag(submrRec.Address, tagLevel, tagName, tagValue);
            } else if (tagName == GEDCOMTagType.LANG) {
                curTag = submrRec.AddLanguage(new GDMLanguage(submrRec, tagName, tagValue));
            } else if (tagName == GEDCOMTagType.RFN) {
                submrRec.RegisteredReference = tagValue;
                curTag = null;
            } else {
                // 'ADDR' defines by default
                return AddRecordTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static void WriteSubmitterRecord(StreamWriter stream, int level, GDMTag tag)
        {
            GDMSubmitterRecord submrRec = (GDMSubmitterRecord)tag;

            WriteRecord(stream, level, submrRec);

            level += 1;
            WritePersonalName(stream, level, submrRec.Name);
            WriteAddress(stream, level, submrRec.Address);
            WriteTagLine(stream, level, GEDCOMTagType.RFN, submrRec.RegisteredReference, true);
        }


        private static StackTuple AddRecordTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMRecord record = (GDMRecord)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.NOTE) {
                curTag = record.Notes.Add(new GDMNotes(record, tagName, tagValue));
            } else if (tagName == GEDCOMTagType.SOUR) {
                curTag = record.SourceCitations.Add(new GDMSourceCitation(record, tagName, tagValue));
                addHandler = AddSourceCitationTag;
            } else if (tagName == GEDCOMTagType.OBJE) {
                curTag = record.MultimediaLinks.Add(new GDMMultimediaLink(record, tagName, tagValue));
                addHandler = AddMultimediaLinkTag;
            } else if (tagName == GEDCOMTagType.REFN) {
                curTag = record.UserReferences.Add(new GDMUserReference(record, tagName, tagValue));
                addHandler = AddUserReferenceTag;
            } else if (tagName == GEDCOMTagType._UID) {
                record.UID = tagValue;
                curTag = null;
            } else if (tagName == GEDCOMTagType.RIN) {
                record.AutomatedRecordID = tagValue;
                curTag = null;
            } else if (tagName == GEDCOMTagType.CHAN) {
                curTag = record.ChangeDate;
                addHandler = AddChangeDateTag;
            } else {
                return AddBaseTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static void WriteRecord(StreamWriter stream, int level, GDMTag tag)
        {
            GDMRecord record = (GDMRecord)tag;

            WriteRecordValue(stream, level, record);

            level += 1;
            if (!DebugWrite) {
                WriteTagLine(stream, level, GEDCOMTagType._UID, record.UID, true);
                WriteChangeDate(stream, level, record.ChangeDate);
            }
            WriteSubTags(stream, level, tag);

            WriteTagLine(stream, level, GEDCOMTagType.RIN, record.AutomatedRecordID, true);
            WriteList(stream, level, record.Notes, WriteTagEx);
            WriteList(stream, level, record.SourceCitations, WriteSourceCitation);
            WriteList(stream, level, record.MultimediaLinks, WriteMultimediaLink);
            WriteList(stream, level, record.UserReferences, WriteUserReference);
        }


        private static StackTuple AddHeaderTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMHeader header = (GDMHeader)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.CHAR) {
                curTag = header.CharacterSet;
                curTag.ParseString(tagValue);
                addHandler = AddHeaderCharSetTag;
            } else if (tagName == GEDCOMTagType.SOUR) {
                curTag = header.Source;
                curTag.ParseString(tagValue);
                addHandler = AddHeaderSourceTag;
            } else if (tagName == GEDCOMTagType.GEDC) {
                curTag = header.GEDCOM;
                curTag.ParseString(tagValue);
                addHandler = AddHeaderGEDCOMTag;
            } else if (tagName == GEDCOMTagType.LANG) {
                header.Language = GEDCOMUtils.GetLanguageVal(tagValue);
            } else if (tagName == GEDCOMTagType.COPR) {
                header.Copyright = tagValue;
            } else if (tagName == GEDCOMTagType.DEST) {
                header.ReceivingSystemName = tagValue;
            } else if (tagName == GEDCOMTagType.PLAC) {
                curTag = header.Place;
                curTag.ParseString(tagValue);
                addHandler = AddPlaceTag;
            } else if (tagName == GEDCOMTagType.SUBM) {
                curTag = header.Submitter;
                curTag.ParseString(tagValue);
            } else if (tagName == GEDCOMTagType.SUBN) {
                curTag = header.Submission;
                curTag.ParseString(tagValue);
            } else if (tagName == GEDCOMTagType.NOTE) {
                curTag = header.Note;
                curTag.ParseString(tagValue);
                addHandler = AddTextTag;
            } else if (tagName == GEDCOMTagType.DATE) {
                DateTime date;
                GEDCOMUtils.ParseDate(header.GetTree(), tagValue, out date);
                header.TransmissionDateTime = date;
                curTag = header;
                addHandler = AddHeaderTag;
            } else if (tagName == GEDCOMTagType.TIME) {
                TimeSpan time;
                GEDCOMUtils.ParseTime(tagValue, out time);
                DateTime date = header.TransmissionDateTime;
                header.TransmissionDateTime = date.Add(time);
            } else if (tagName == GEDCOMTagType.FILE) {
                curTag = header.File;
                curTag.ParseString(tagValue);
                addHandler = AddHeaderFileTag;
            } else {
                return AddBaseTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static bool WriteHeader(StreamWriter stream, int level, GDMTag tag)
        {
            GDMHeader header = (GDMHeader)tag;

            if (!WriteBaseTag(stream, level, header)) return false;

            level += 1;
            WriteHeaderSource(stream, level, header.Source);
            WriteTagLine(stream, level, GEDCOMTagType.DEST, header.ReceivingSystemName, true);
            WriteHeaderCharSet(stream, level, header.CharacterSet);
            WriteTagLine(stream, level, GEDCOMTagType.LANG, GEDCOMUtils.GetLanguageStr(header.Language), true);
            WriteHeaderGEDCOM(stream, level, header.GEDCOM);
            WriteHeaderFile(stream, level, header.File);
            WriteDateTime(stream, level, header.TransmissionDateTime);
            WriteTagLine(stream, level, GEDCOMTagType.COPR, header.Copyright, true);
            WritePlace(stream, level, header.Place);
            WriteBaseTag(stream, level, header.Submitter);
            WriteBaseTag(stream, level, header.Submission);
            WriteText(stream, level, header.Note);
            return true;
        }


        private static StackTuple AddHeaderSourceTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMHeaderSource headerSource = (GDMHeaderSource)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.VERS) {
                headerSource.Version = tagValue;
            } else if (tagName == GEDCOMTagType.NAME) {
                headerSource.ProductName = tagValue;
            } else {
                return AddBaseTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static bool WriteHeaderSource(StreamWriter stream, int level, GDMTag tag)
        {
            GDMHeaderSource headerSource = (GDMHeaderSource)tag;

            if (!WriteBaseTag(stream, level, headerSource)) return false;

            level += 1;
            WriteTagLine(stream, level, GEDCOMTagType.VERS, headerSource.Version, true);
            WriteTagLine(stream, level, GEDCOMTagType.NAME, headerSource.ProductName, true);
            return true;
        }


        private static StackTuple AddHeaderGEDCOMTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMHeaderGEDCOM headerGEDCOM = (GDMHeaderGEDCOM)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.VERS) {
                headerGEDCOM.Version = tagValue;
            } else if (tagName == GEDCOMTagType.FORM) {
                headerGEDCOM.Form = tagValue;
            } else {
                return AddBaseTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static bool WriteHeaderGEDCOM(StreamWriter stream, int level, GDMTag tag)
        {
            GDMHeaderGEDCOM headerGEDCOM = (GDMHeaderGEDCOM)tag;

            if (!WriteBaseTag(stream, level, headerGEDCOM)) return false;

            level += 1;
            WriteTagLine(stream, level, GEDCOMTagType.VERS, headerGEDCOM.Version, true);
            WriteTagLine(stream, level, GEDCOMTagType.FORM, headerGEDCOM.Form, true);
            return true;
        }


        private static StackTuple AddHeaderCharSetTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMHeaderCharSet headerCharSet = (GDMHeaderCharSet)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.VERS) {
                headerCharSet.Version = tagValue;
            } else {
                return AddBaseTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static bool WriteHeaderCharSet(StreamWriter stream, int level, GDMTag tag)
        {
            GDMHeaderCharSet headerCharSet = (GDMHeaderCharSet)tag;

            if (!WriteBaseTag(stream, level, headerCharSet)) return false;

            level += 1;
            WriteTagLine(stream, level, GEDCOMTagType.VERS, headerCharSet.Version, true);
            return true;
        }


        private static StackTuple AddHeaderFileTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMHeaderFile headerFile = (GDMHeaderFile)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType._REV) {
                headerFile.Revision = GEDCOMUtils.GetIntVal(tagValue);
            } else {
                return AddBaseTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static bool WriteHeaderFile(StreamWriter stream, int level, GDMTag tag)
        {
            GDMHeaderFile headerFile = (GDMHeaderFile)tag;

            if (!WriteBaseTag(stream, level, headerFile)) return false;

            level += 1;
            WriteTagLine(stream, level, GEDCOMTagType._REV, GEDCOMUtils.GetIntStr(headerFile.Revision), true);
            return true;
        }


        private static StackTuple AddChangeDateTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMChangeDate changeDate = (GDMChangeDate)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.DATE) {
                DateTime date;
                GEDCOMUtils.ParseDate(changeDate.GetTree(), tagValue, out date);
                changeDate.ChangeDateTime = date;
                curTag = changeDate;
            } else if (tagName == GEDCOMTagType.TIME) {
                TimeSpan time;
                GEDCOMUtils.ParseTime(tagValue, out time);
                DateTime date = changeDate.ChangeDateTime;
                changeDate.ChangeDateTime = date.Add(time);
            } else if (tagName == GEDCOMTagType.NOTE) {
                curTag = changeDate.AddTag(new GDMNotes(changeDate, tagName, tagValue));
            } else {
                return AddBaseTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static bool WriteChangeDate(StreamWriter stream, int level, GDMTag tag)
        {
            GDMChangeDate changeDate = (GDMChangeDate)tag;

            if (!WriteBaseTag(stream, level, changeDate)) return false;

            level += 1;
            WriteDateTime(stream, level, changeDate.ChangeDateTime);
            return true;
        }

        private static void WriteDateTime(StreamWriter stream, int level, DateTime dtx)
        {
            if (!dtx.Equals(GDMChangeDate.ZeroDateTime)) {
                WriteTagLine(stream, level, GEDCOMTagType.DATE, GEDCOMUtils.GetDateStr(dtx), true);
                WriteTagLine(stream, ++level, GEDCOMTagType.TIME, GEDCOMUtils.GetTimeStr(dtx.TimeOfDay), true);
            }
        }


        private static StackTuple AddCustomEventTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMCustomEvent custEvent = (GDMCustomEvent)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.ADDR) {
                curTag = custEvent.Address;
                curTag.ParseString(tagValue);
                addHandler = AddAddressTag;
            } else if (tagName == GEDCOMTagType.AGNC) {
                custEvent.Agency = tagValue;
            } else if (tagName == GEDCOMTagType.CAUS) {
                custEvent.Cause = tagValue;
            } else if (tagName == GEDCOMTagType.TYPE) {
                custEvent.Classification = tagValue;
            } else if (tagName == GEDCOMTagType.DATE) {
                curTag = custEvent.Date;
                curTag.ParseString(tagValue);
            } else if (tagName == GEDCOMTagType.PLAC) {
                curTag = custEvent.Place;
                curTag.ParseString(tagValue);
                addHandler = AddPlaceTag;
            } else if (tagName == GEDCOMTagType.RELI) {
                custEvent.ReligiousAffilation = tagValue;
            } else if (tagName == GEDCOMTagType.RESN) {
                custEvent.Restriction = GEDCOMUtils.GetRestrictionVal(tagValue);
            } else if (tagName == GEDCOMTagType.PHON || tagName == GEDCOMTagType.EMAIL || tagName == GEDCOMTagType.FAX || tagName == GEDCOMTagType.WWW) {
                return AddAddressTag(custEvent.Address, tagLevel, tagName, tagValue);
            } else {
                // define 'PLAC', 'ADDR', 'DATE' by default
                return AddTagWithListsTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static bool WriteCustomEvent(StreamWriter stream, int level, GDMTag tag)
        {
            GDMCustomEvent custEvent = (GDMCustomEvent)tag;

            if (!WriteBaseTag(stream, level, custEvent)) return false;

            level += 1;
            WriteTagLine(stream, level, GEDCOMTagType.TYPE, custEvent.Classification, true);
            WriteBaseTag(stream, level, custEvent.Date);
            WritePlace(stream, level, custEvent.Place);
            WriteAddress(stream, level, custEvent.Address);
            WriteTagLine(stream, level, GEDCOMTagType.CAUS, custEvent.Cause, true);
            WriteTagLine(stream, level, GEDCOMTagType.AGNC, custEvent.Agency, true);
            WriteTagLine(stream, level, GEDCOMTagType.RELI, custEvent.ReligiousAffilation, true);
            WriteTagLine(stream, level, GEDCOMTagType.RESN, GEDCOMUtils.GetRestrictionStr(custEvent.Restriction), true);

            WriteList(stream, level, custEvent.Notes, WriteTagEx);
            WriteList(stream, level, custEvent.SourceCitations, WriteSourceCitation);
            WriteList(stream, level, custEvent.MultimediaLinks, WriteMultimediaLink);
            return true;
        }


        private static StackTuple AddSourceDataTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMSourceData sourData = (GDMSourceData)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.EVEN) {
                curTag = sourData.Events.Add(new GDMSourceEvent(sourData, tagName, tagValue));
                addHandler = AddSourceDataEventTag;
            } else if (tagName == GEDCOMTagType.AGNC) {
                sourData.Agency = tagValue;
            } else {
                return AddTagWithListsTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static bool WriteSourceData(StreamWriter stream, int level, GDMTag tag)
        {
            GDMSourceData sourData = (GDMSourceData)tag;

            if (!WriteTagWithLists(stream, level, tag)) return false;

            level += 1;
            WriteTagLine(stream, level, GEDCOMTagType.AGNC, sourData.Agency, true);
            WriteList(stream, level, sourData.Events, WriteSourceDataEvent);
            return true;
        }


        private static StackTuple AddSourceDataEventTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMSourceEvent dataEvent = (GDMSourceEvent)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.DATE) {
                curTag = dataEvent.Date;
                curTag.ParseString(tagValue);
            } else if (tagName == GEDCOMTagType.PLAC) {
                curTag = dataEvent.Place;
                curTag.ParseString(tagValue);
                addHandler = AddPlaceTag;
            } else {
                return AddBaseTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static bool WriteSourceDataEvent(StreamWriter stream, int level, GDMTag tag)
        {
            GDMSourceEvent dataEvent = (GDMSourceEvent)tag;

            if (!WriteBaseTag(stream, level, dataEvent)) return false;

            level += 1;
            WriteBaseTag(stream, level, dataEvent.Date);
            WritePlace(stream, level, dataEvent.Place);
            return true;
        }

        private static StackTuple AddPointerWithNotesTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMPointerWithNotes ptrWN = (GDMPointerWithNotes)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.NOTE) {
                curTag = ptrWN.Notes.Add(new GDMNotes(ptrWN, tagName, tagValue));
            } else {
                return AddBaseTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }


        private static StackTuple AddTagWithListsTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMTagWithLists tagWL = (GDMTagWithLists)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.NOTE) {
                curTag = tagWL.Notes.Add(new GDMNotes(tagWL, tagName, tagValue));
            } else if (tagName == GEDCOMTagType.SOUR) {
                curTag = tagWL.SourceCitations.Add(new GDMSourceCitation(tagWL, tagName, tagValue));
                addHandler = AddSourceCitationTag;
            } else if (tagName == GEDCOMTagType.OBJE) {
                curTag = tagWL.MultimediaLinks.Add(new GDMMultimediaLink(tagWL, tagName, tagValue));
                addHandler = AddMultimediaLinkTag;
            } else {
                return AddBaseTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static bool WriteTagWithLists(StreamWriter stream, int level, GDMTag tag)
        {
            GDMTagWithLists tagWL = (GDMTagWithLists)tag;

            if (!WriteBaseTag(stream, level, tagWL)) return false;

            level += 1;
            WriteList(stream, level, tagWL.Notes, WriteTagEx);
            WriteList(stream, level, tagWL.SourceCitations, WriteSourceCitation);
            WriteList(stream, level, tagWL.MultimediaLinks, WriteMultimediaLink);
            return true;
        }


        private static StackTuple AddTextTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMTextTag textTag = (GDMTextTag)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            var strings = textTag.Lines;
            if (tagName == GEDCOMTagType.CONC) {
                int strCount = strings.Count;
                if (strCount > 0) {
                    strings[strCount - 1] = strings[strCount - 1] + tagValue;
                } else {
                    strings.Add(tagValue);
                }
            } else if (tagName == GEDCOMTagType.CONT) {
                strings.Add(tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static bool WriteText(StreamWriter stream, int level, IGDMTextObject textTag)
        {
            if (textTag.IsEmpty()) return false;

            var strings = textTag.Lines;
            int strCount = strings.Count;
            for (int i = 0; i < strCount; i++) {
                string str = strings[i];

                int len = Math.Min(str.Length, GEDCOMProvider.MAX_LINE_LENGTH);
                string sub = str.Substring(0, len);
                str = str.Remove(0, len);

                if (i == 0/* && !isRecordTag*/) {
                    WriteTagLine(stream, level, textTag.Name, sub);
                    level += 1;
                } else {
                    WriteTagLine(stream, level, GEDCOMTagType.CONT, sub);
                }

                while (str.Length > 0) {
                    len = Math.Min(str.Length, GEDCOMProvider.MAX_LINE_LENGTH);

                    WriteTagLine(stream, level, GEDCOMTagType.CONC, str.Substring(0, len));

                    str = str.Remove(0, len);
                }
            }

            return true;
        }


        private static void WriteSubTags(StreamWriter stream, int level, GDMTag tag)
        {
            var subTags = tag.SubTags;

            int subtagsCount = subTags.Count;
            if (subtagsCount > 0) {
                for (int i = 0; i < subtagsCount; i++) {
                    GDMTag subtag = subTags[i];
                    if (subtag.Name == GEDCOMTagType.CONC || subtag.Name == GEDCOMTagType.CONT) {
                        WriteBaseTag(stream, level, subtag);
                    }
                }

                for (int i = 0; i < subtagsCount; i++) {
                    GDMTag subtag = subTags[i];
                    string tagName = subtag.Name;
                    if (tagName != GEDCOMTagType.CONT && tagName != GEDCOMTagType.CONC) {
                        WriteTagEx(stream, level, subtag);
                    }
                }
            }
        }

        private static void WriteRecordValue(StreamWriter stream, int level, GDMRecord record)
        {
            string str = level.ToString();

            if (!string.IsNullOrEmpty(record.XRef)) {
                str = str + " @" + record.XRef + "@";
            }

            str = str + " " + record.Name;

            string strValue = record.StringValue;
            if (!string.IsNullOrEmpty(strValue)) {
                str = str + " " + strValue;
            }

            stream.Write(str + GEDCOMProvider.GEDCOM_NEWLINE);
        }

        private static void WriteTagValue(StreamWriter stream, int level, GDMTag tag)
        {
            WriteTagLine(stream, level, tag.Name, tag.StringValue);
        }

        public static bool WriteTagEx(StreamWriter stream, int level, GDMTag tag)
        {
            bool result;
            TagInfo tagInfo = GEDCOMFactory.GetInstance().GetTagInfo(tag.Name);
            if (tagInfo == null) {
                result = WriteBaseTag(stream, level, tag);
            } else {
                SaveTagHandler saveHandler = tagInfo.SaveHandler;
                if (saveHandler == null) {
                    result = WriteBaseTag(stream, level, tag);
                } else {
                    result = saveHandler(stream, level, tag);
                }
            }
            return result;
        }

        internal static StackTuple AddBaseTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMTag ownerTag = (GDMTag)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            curTag = ownerTag.AddTag(tagName, tagValue, null);

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static bool WriteBaseTag(StreamWriter stream, int level, GDMTag tag)
        {
            if (tag.IsEmpty() && GEDCOMProvider.SkipEmptyTag(tag.Name)) return false;

            WriteTagValue(stream, level, tag);
            WriteSubTags(stream, ++level, tag);
            return true;
        }

        private static void WriteList<T>(StreamWriter stream, int level, GDMList<T> list, SaveTagHandler tagHandler) where T : GDMTag
        {
            if (tagHandler == null) throw new ArgumentException("tagHandler");

            IList<T> internalList = list.GetList();
            if (internalList == null) return;

            int num = internalList.Count;
            for (int i = 0; i < num; i++) {
                var item = internalList[i];
                if (item != null) {
                    tagHandler(stream, level, item);
                }
            }
        }

        // debug field
        public static bool DebugWrite = false;

        private static void WriteTagLine(StreamWriter stream, int level, string tagName, string tagValue, bool skipEmpty = false)
        {
            bool isEmpty = string.IsNullOrEmpty(tagValue);
            if (string.IsNullOrEmpty(tagName) || (isEmpty && skipEmpty)) return;

            string str = level + " " + tagName;
            if (!string.IsNullOrEmpty(tagValue)) {
                str = str + " " + tagValue;
            }
            stream.Write(str + GEDCOM_NEWLINE);
        }

        private static StackTuple CreateReaderStackTuple(int level, GDMTag tag, AddTagHandler addHandler)
        {
            if (tag == null) {
                return null;
            } else {
                if (addHandler == null) {
                    addHandler = GEDCOMFactory.GetInstance().GetAddHandler(tag.Name);
                }
                return new StackTuple(level, tag, addHandler);
            }
        }


        private static StackTuple AddLocationRecordTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMLocationRecord locRec = (GDMLocationRecord)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.NAME) {
                locRec.LocationName = tagValue;
                curTag = null;
            } else if (tagName == GEDCOMTagType.MAP) {
                curTag = locRec.Map;
                addHandler = AddMapTag;
            } else {
                return AddRecordTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static void WriteLocationRecord(StreamWriter stream, int level, GDMTag tag)
        {
            GDMLocationRecord locRec = (GDMLocationRecord)tag;

            WriteRecord(stream, level, locRec);

            level += 1;
            WriteMap(stream, level, locRec.Map);
            WriteTagLine(stream, level, GEDCOMTagType.NAME, locRec.LocationName, true);
        }


        private static StackTuple AddPlaceTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMPlace place = (GDMPlace)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.FORM) {
                place.Form = tagValue;
                curTag = null;
            } else if (tagName == GEDCOMTagType.MAP) {
                curTag = place.Map;
                addHandler = AddMapTag;
            } else {
                return AddTagWithListsTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static bool WritePlace(StreamWriter stream, int level, GDMTag tag)
        {
            GDMPlace place = (GDMPlace)tag;

            if (!WriteTagWithLists(stream, level, tag)) return false;

            level += 1;
            WriteMap(stream, level, place.Map);
            WriteTagLine(stream, level, GEDCOMTagType.FORM, place.Form, true);
            return true;
        }


        private static StackTuple AddMapTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMMap map = (GDMMap)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.LATI) {
                map.Lati = GEDCOMUtils.GetGeoCoord(tagValue, GeoCoord.Lati);
                curTag = null;
            } else if (tagName == GEDCOMTagType.LONG) {
                map.Long = GEDCOMUtils.GetGeoCoord(tagValue, GeoCoord.Long);
                curTag = null;
            } else {
                return AddBaseTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static bool WriteMap(StreamWriter stream, int level, GDMTag tag)
        {
            GDMMap map = (GDMMap)tag;
            if (map.IsEmpty() && GEDCOMProvider.SkipEmptyTag(map.Name)) return false;

            WriteTagValue(stream, level, tag);

            level += 1;
            WriteTagLine(stream, level, GEDCOMTagType.LATI, GEDCOMUtils.GetFloatStr(map.Lati), true);
            WriteTagLine(stream, level, GEDCOMTagType.LONG, GEDCOMUtils.GetFloatStr(map.Long), true);
            return true;
        }


        private static StackTuple AddUserReferenceTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMUserReference userRef = (GDMUserReference)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.TYPE) {
                userRef.ReferenceType = tagValue;
                curTag = null;
            } else {
                return AddBaseTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static bool WriteUserReference(StreamWriter stream, int level, GDMTag tag)
        {
            GDMUserReference userRef = (GDMUserReference)tag;

            if (!WriteBaseTag(stream, level, userRef)) return false;

            GEDCOMProvider.WriteTagLine(stream, ++level, GEDCOMTagType.TYPE, userRef.ReferenceType, true);
            return true;
        }


        // Format: FORM\TYPE
        private static StackTuple AddFileReferenceWithTitleTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMFileReferenceWithTitle fileRef = (GDMFileReferenceWithTitle)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.TITL) {
                fileRef.Title = tagValue;
                curTag = null;
            } else if (tagName == GEDCOMTagType.FORM) {
                fileRef.MultimediaFormat = GEDCOMUtils.GetMultimediaFormatVal(tagValue);
                curTag = fileRef;
                addHandler = AddFileReferenceWithTitleTag;
            } else if (tagName == GEDCOMTagType.TYPE) {
                fileRef.MediaType = GEDCOMUtils.GetMediaTypeVal(tagValue);
                curTag = null;
            } else {
                return AddBaseTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        // Format: FORM\TYPE
        private static bool WriteFileReferenceWithTitle(StreamWriter stream, int level, GDMTag tag)
        {
            GDMFileReferenceWithTitle fileRef = (GDMFileReferenceWithTitle)tag;

            if (!WriteBaseTag(stream, level, fileRef)) return false;

            level += 1;
            GEDCOMProvider.WriteTagLine(stream, level, GEDCOMTagType.FORM, GEDCOMUtils.GetMultimediaFormatStr(fileRef.MultimediaFormat), true);
            GEDCOMProvider.WriteTagLine(stream, (level+1), GEDCOMTagType.TYPE, GEDCOMUtils.GetMediaTypeStr(fileRef.MediaType), true);
            GEDCOMProvider.WriteTagLine(stream, level, GEDCOMTagType.TITL, fileRef.Title, true);
            return true;
        }


        // Format: FORM\MEDI
        private static StackTuple AddFileReferenceTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMFileReference fileRef = (GDMFileReference)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.FORM) {
                fileRef.MultimediaFormat = GEDCOMUtils.GetMultimediaFormatVal(tagValue);
                curTag = fileRef;
                addHandler = AddFileReferenceTag;
            } else if (tagName == GEDCOMTagType.MEDI) {
                fileRef.MediaType = GEDCOMUtils.GetMediaTypeVal(tagValue);
                curTag = null;
            } else {
                return AddBaseTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        // Format: FORM\MEDI
        private static bool WriteFileReference(StreamWriter stream, int level, GDMTag tag)
        {
            GDMFileReference fileRef = (GDMFileReference)tag;

            if (!WriteBaseTag(stream, level, fileRef)) return false;

            level += 1;
            GEDCOMProvider.WriteTagLine(stream, level, GEDCOMTagType.FORM, GEDCOMUtils.GetMultimediaFormatStr(fileRef.MultimediaFormat), true);
            GEDCOMProvider.WriteTagLine(stream, ++level, GEDCOMTagType.MEDI, GEDCOMUtils.GetMediaTypeStr(fileRef.MediaType), true);
            return true;
        }


        private static StackTuple AddMultimediaLinkTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMMultimediaLink mmLink = (GDMMultimediaLink)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.TITL) {
                mmLink.Title = tagValue;
            } else if (tagName == GEDCOMTagType._PRIM) {
                mmLink.IsPrimary = GEDCOMUtils.GetBoolVal(tagValue);
            } else if (tagName == GEDCOMTagType._PRIM_CUTOUT) {
                mmLink.IsPrimaryCutout = GEDCOMUtils.GetBoolVal(tagValue);
            } else if (tagName == GEDCOMTagType._POSITION) {
                curTag = mmLink.CutoutPosition;
            } else if (tagName == GEDCOMTagType.FILE) {
                curTag = mmLink.FileReferences.Add(new GDMFileReference(mmLink, tagName, tagValue));
            } else {
                return AddBaseTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        public static bool WriteMultimediaLink(StreamWriter stream, int level, GDMTag tag)
        {
            GDMMultimediaLink mmLink = (GDMMultimediaLink)tag;

            if (!WriteBaseTag(stream, level, mmLink)) return false;

            level += 1;
            WriteList(stream, level, mmLink.FileReferences, WriteFileReference);
            WriteTagLine(stream, level, GEDCOMTagType.TITL, mmLink.Title, true);
            if (mmLink.IsPrimary) WriteTagLine(stream, level, GEDCOMTagType._PRIM, GEDCOMUtils.GetBoolStr(mmLink.IsPrimary), true);
            if (mmLink.IsPrimaryCutout) WriteTagLine(stream, level, GEDCOMTagType._PRIM_CUTOUT, GEDCOMUtils.GetBoolStr(mmLink.IsPrimaryCutout), true);
            WriteBaseTag(stream, level, mmLink.CutoutPosition);
            return true;
        }


        private static StackTuple AddSourceCitationTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMSourceCitation sourCit = (GDMSourceCitation)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.QUAY) {
                sourCit.CertaintyAssessment = GEDCOMUtils.GetIntVal(tagValue);
            } else if (tagName == GEDCOMTagType.PAGE) {
                sourCit.Page = tagValue;
            } else {
                return AddBaseTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        public static bool WriteSourceCitation(StreamWriter stream, int level, GDMTag tag)
        {
            GDMSourceCitation sourCit = (GDMSourceCitation)tag;

            if (!WriteBaseTag(stream, level, sourCit)) return false;

            level += 1;
            WriteTagLine(stream, level, GEDCOMTagType.PAGE, sourCit.Page, true);
            WriteTagLine(stream, level, GEDCOMTagType.QUAY, GEDCOMUtils.GetIntStr(sourCit.CertaintyAssessment), true);
            return true;
        }


        private static StackTuple AddAssociationTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMAssociation asso = (GDMAssociation)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.RELA) {
                asso.Relation = tagValue;
                curTag = null;
            } else if (tagName == GEDCOMTagType.SOUR) {
                curTag = asso.SourceCitations.Add(new GDMSourceCitation(asso, tagName, tagValue));
                addHandler = AddSourceCitationTag;
            } else {
                return AddPointerWithNotesTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static bool WriteAssociation(StreamWriter stream, int level, GDMTag tag)
        {
            GDMAssociation asso = (GDMAssociation)tag;

            if (!WriteBaseTag(stream, level, asso)) return false;

            level += 1;
            WriteTagLine(stream, level, GEDCOMTagType.RELA, asso.Relation, true);
            WriteList(stream, level, asso.SourceCitations, WriteSourceCitation);
            return true;
        }


        private static StackTuple AddAddressTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMAddress addr = (GDMAddress)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.CONT) {
                addr.Lines.Add(tagValue);
            } else if (tagName == GEDCOMTagType.ADR1) {
                addr.AddressLine1 = tagValue;
            } else if (tagName == GEDCOMTagType.ADR2) {
                addr.AddressLine2 = tagValue;
            } else if (tagName == GEDCOMTagType.ADR3) {
                addr.AddressLine3 = tagValue;
            } else if (tagName == GEDCOMTagType.CITY) {
                addr.AddressCity = tagValue;
            } else if (tagName == GEDCOMTagType.STAE) {
                addr.AddressState = tagValue;
            } else if (tagName == GEDCOMTagType.POST) {
                addr.AddressPostalCode = tagValue;
            } else if (tagName == GEDCOMTagType.CTRY) {
                addr.AddressCountry = tagValue;
            } else if (tagName == GEDCOMTagType.PHON) {
                curTag = addr.AddPhoneNumber(tagValue);
            } else if (tagName == GEDCOMTagType.EMAIL) {
                curTag = addr.AddEmailAddress(tagValue);
            } else if (tagName == GEDCOMTagType.FAX) {
                curTag = addr.AddFaxNumber(tagValue);
            } else if (tagName == GEDCOMTagType.WWW) {
                curTag = addr.AddWebPage(tagValue);
            } else {
                return AddBaseTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static bool WriteAddress(StreamWriter stream, int level, GDMTag tag)
        {
            GDMAddress addr = (GDMAddress)tag;
            if (addr.IsEmpty() && GEDCOMProvider.SkipEmptyTag(addr.Name)) return false;

            WriteTagValue(stream, level, addr);

            int lev = level + 1;

            var strings = addr.Lines;
            int strCount = strings.Count;
            for (int i = 1; i < strCount; i++) {
                WriteTagLine(stream, lev, GEDCOMTagType.CONT, strings[i]);
            }

            WriteTagLine(stream, lev, GEDCOMTagType.ADR1, addr.AddressLine1, true);
            WriteTagLine(stream, lev, GEDCOMTagType.ADR2, addr.AddressLine2, true);
            WriteTagLine(stream, lev, GEDCOMTagType.ADR3, addr.AddressLine3, true);
            WriteTagLine(stream, lev, GEDCOMTagType.CITY, addr.AddressCity, true);
            WriteTagLine(stream, lev, GEDCOMTagType.STAE, addr.AddressState, true);
            WriteTagLine(stream, lev, GEDCOMTagType.CTRY, addr.AddressCountry, true);
            WriteTagLine(stream, lev, GEDCOMTagType.POST, addr.AddressPostalCode, true);

            // same level
            WriteList(stream, level, addr.PhoneNumbers, WriteTagEx);
            WriteList(stream, level, addr.EmailAddresses, WriteTagEx);
            WriteList(stream, level, addr.FaxNumbers, WriteTagEx);
            WriteList(stream, level, addr.WebPages, WriteTagEx);
            return true;
        }


        private static StackTuple AddChildToFamilyLinkTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMChildToFamilyLink cfl = (GDMChildToFamilyLink)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.STAT) {
                cfl.ChildLinkageStatus = GEDCOMUtils.GetChildLinkageStatusVal(tagValue);
                curTag = null;
            } else if (tagName == GEDCOMTagType.PEDI) {
                cfl.PedigreeLinkageType = GEDCOMUtils.GetPedigreeLinkageTypeVal(tagValue);
                curTag = null;
            } else {
                return AddPointerWithNotesTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static bool WriteChildToFamilyLink(StreamWriter stream, int level, GDMTag tag)
        {
            GDMChildToFamilyLink cfl = (GDMChildToFamilyLink)tag;

            if (!WriteBaseTag(stream, level, cfl)) return false;

            level += 1;
            WriteTagLine(stream, level, GEDCOMTagType.STAT, GEDCOMUtils.GetChildLinkageStatusStr(cfl.ChildLinkageStatus), true);
            WriteTagLine(stream, level, GEDCOMTagType.PEDI, GEDCOMUtils.GetPedigreeLinkageTypeStr(cfl.PedigreeLinkageType), true);
            return true;
        }


        private static StackTuple AddPersonalNameTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMPersonalName persName = (GDMPersonalName)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.TYPE) {
                persName.NameType = GEDCOMUtils.GetNameTypeVal(tagValue);
            } else if (tagName == GEDCOMTagType._LANG || tagName == GEDCOMTagType.LANG) {
                persName.Language = GEDCOMUtils.GetLanguageVal(tagValue);
            } else if (tagName == GEDCOMTagType.FONE || tagName == GEDCOMTagType.ROMN) {
                return AddBaseTag(owner, tagLevel, tagName, tagValue);
            } else {
                return AddPersonalNamePiecesTag(persName.Pieces, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        public static bool WritePersonalName(StreamWriter stream, int level, GDMTag tag)
        {
            GDMPersonalName persName = (GDMPersonalName)tag;

            if (!WriteBaseTag(stream, level, persName)) return false;

            int lev = level + 1;
            WriteTagLine(stream, lev, GEDCOMTagType.LANG, GEDCOMUtils.GetLanguageStr(persName.Language), true);
            WriteTagLine(stream, lev, GEDCOMTagType.TYPE, GEDCOMUtils.GetNameTypeStr(persName.NameType), true);
            WritePersonalNamePieces(stream, level, persName.Pieces); // same level
            return true;
        }


        private static StackTuple AddPersonalNamePiecesTag(GDMObject owner, int tagLevel, string tagName, string tagValue)
        {
            GDMPersonalNamePieces persNamePieces = (GDMPersonalNamePieces)owner;
            GDMTag curTag = null;
            AddTagHandler addHandler = null;

            if (tagName == GEDCOMTagType.NPFX) {
                persNamePieces.Prefix = tagValue;
            } else if (tagName == GEDCOMTagType.GIVN) {
                persNamePieces.Given = tagValue;
            } else if (tagName == GEDCOMTagType.NICK) {
                persNamePieces.Nickname = tagValue;
            } else if (tagName == GEDCOMTagType.SPFX) {
                persNamePieces.SurnamePrefix = tagValue;
            } else if (tagName == GEDCOMTagType.SURN) {
                persNamePieces.Surname = tagValue;
            } else if (tagName == GEDCOMTagType.NSFX) {
                persNamePieces.Suffix = tagValue;
            } else if (tagName == GEDCOMTagType._PATN) {
                persNamePieces.PatronymicName = tagValue;
            } else if (tagName == GEDCOMTagType._MARN) {
                persNamePieces.MarriedName = tagValue;
            } else if (tagName == GEDCOMTagType._RELN) {
                persNamePieces.ReligiousName = tagValue;
            } else if (tagName == GEDCOMTagType._CENN) {
                persNamePieces.CensusName = tagValue;
            } else {
                return AddBaseTag(owner, tagLevel, tagName, tagValue);
            }

            return CreateReaderStackTuple(tagLevel, curTag, addHandler);
        }

        private static void WritePersonalNamePieces(StreamWriter stream, int level, GDMTag tag)
        {
            GDMPersonalNamePieces persNamePieces = (GDMPersonalNamePieces)tag;

            // without NameValue
            int lev = level + 1;
            WriteSubTags(stream, lev, persNamePieces);

            WriteList(stream, level, persNamePieces.Notes, WriteTagEx);
            WriteList(stream, level, persNamePieces.SourceCitations, WriteSourceCitation);

            WriteTagLine(stream, lev, GEDCOMTagType.SURN, persNamePieces.Surname, true);
            WriteTagLine(stream, lev, GEDCOMTagType.GIVN, persNamePieces.Given, true);
            WriteTagLine(stream, lev, GEDCOMTagType._PATN, persNamePieces.PatronymicName, true);
            WriteTagLine(stream, lev, GEDCOMTagType.NPFX, persNamePieces.Prefix, true);
            WriteTagLine(stream, lev, GEDCOMTagType.NICK, persNamePieces.Nickname, true);
            WriteTagLine(stream, lev, GEDCOMTagType.SPFX, persNamePieces.SurnamePrefix, true);
            WriteTagLine(stream, lev, GEDCOMTagType.NSFX, persNamePieces.Suffix, true);
            WriteTagLine(stream, lev, GEDCOMTagType._MARN, persNamePieces.MarriedName, true);
            WriteTagLine(stream, lev, GEDCOMTagType._RELN, persNamePieces.ReligiousName, true);
            WriteTagLine(stream, lev, GEDCOMTagType._CENN, persNamePieces.CensusName, true);

            // FIXME: transfer to this order in future
            /*WriteTagLine(stream, level, GEDCOMTagType.NPFX, persNamePieces.Prefix, true);
            WriteTagLine(stream, level, GEDCOMTagType.GIVN, persNamePieces.Given, true);
            WriteTagLine(stream, level, GEDCOMTagType.NICK, persNamePieces.Nickname, true);
            WriteTagLine(stream, level, GEDCOMTagType.SPFX, persNamePieces.SurnamePrefix, true);
            WriteTagLine(stream, level, GEDCOMTagType.SURN, persNamePieces.Surname, true);
            WriteTagLine(stream, level, GEDCOMTagType.NSFX, persNamePieces.Suffix, true);
            WriteTagLine(stream, level, GEDCOMTagType._PATN, persNamePieces.PatronymicName, true);
            WriteTagLine(stream, level, GEDCOMTagType._MARN, persNamePieces.MarriedName, true);
            WriteTagLine(stream, level, GEDCOMTagType._RELN, persNamePieces.ReligiousName, true);
            WriteTagLine(stream, level, GEDCOMTagType._CENN, persNamePieces.CensusName, true);*/
        }

        #endregion

        #region Format variations

        /// <summary>
        /// Fix of errors that are in the dates of FamilyTreeBuilder.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string FixFTB(string str)
        {
            string result = str;
            string su = result.Substring(0, 3).ToUpperInvariant();

            if (su == GDMCustomDate.GEDCOMDateRangeArray[0] ||
                su == GDMCustomDate.GEDCOMDateRangeArray[1] ||
                su == GDMCustomDate.GEDCOMDateApproximatedArray[1] ||
                su == GDMCustomDate.GEDCOMDateApproximatedArray[2] ||
                su == GDMCustomDate.GEDCOMDateApproximatedArray[3])
            {
                result = result.Remove(0, 4);
            }
            return result;
        }

        /// <summary>
        /// Fix of line errors that are in the files of FamilyTreeBuilder.
        /// </summary>
        private static void FixFTBLine(GDMTag curRecord, GDMTag curTag, int lineNum, string str)
        {
            try {
                if (curTag.Name == GEDCOMTagType.CONT) {
                    curTag.StringValue += " " + str;
                } else if (curTag is GDMNotes) {
                    curTag.AddTag(GEDCOMTagType.CONT, str, null);
                } else {
                    if (curRecord != null) {
                        curRecord.AddTag(GEDCOMTagType.NOTE, str, null);
                    }
                }
            } catch (Exception ex) {
                Logger.LogWrite("GEDCOMProvider.FixFTBLine(): Line " + lineNum.ToString() + " failed correct: " + ex.Message);
            }
        }

        public static GEDCOMFormat GetGEDCOMFormat(GDMTree tree)
        {
            if (tree != null) {
                string sour = tree.Header.Source.StringValue.Trim();

                int num = GEDCOMProvider.GEDCOMFormats.Length;
                for (int i = 1; i < num; i++) {
                    var appFmt = GEDCOMProvider.GEDCOMFormats[i];
                    if (string.Equals(appFmt.Sign, sour, StringComparison.Ordinal)) {
                        return appFmt.Format;
                    }
                }
            }

            return GEDCOMFormat.gf_Unknown;
        }

        public static GEDCOMAppFormat GetGEDCOMFormatProps(GEDCOMFormat format)
        {
            int num = GEDCOMProvider.GEDCOMFormats.Length;
            for (int i = 1; i < num; i++) {
                var appFmt = GEDCOMProvider.GEDCOMFormats[i];
                if (appFmt.Format == format) {
                    return appFmt;
                }
            }

            return GEDCOMProvider.GEDCOMFormats[0];
        }

        #endregion

        #region Tag properties

        static GEDCOMProvider()
        {
            GEDCOMFormats = new GEDCOMAppFormat[] {
                new GEDCOMAppFormat(GEDCOMFormat.gf_Unknown, "", "", -1),
                new GEDCOMAppFormat(GEDCOMFormat.gf_Native, "GEDKeeper", "", -1),
                new GEDCOMAppFormat(GEDCOMFormat.gf_Genealogy_RusOld, "├σφσαδεπΦ", "Genealogy (Rus, old)", 1251), // signature in CP437

                new GEDCOMAppFormat(GEDCOMFormat.gf_AGES, "AGES", "Ages!", -1),
                new GEDCOMAppFormat(GEDCOMFormat.gf_ALTREE, "ALTREE", "Agelong Tree", -1),
                new GEDCOMAppFormat(GEDCOMFormat.gf_Ahnenblatt, "AHN", "Ahnenblatt", -1),
                new GEDCOMAppFormat(GEDCOMFormat.gf_AncestQuest, "AncestQuest", "Ancestral Quest", -1),
                new GEDCOMAppFormat(GEDCOMFormat.gf_EasyTree, "EasyTree", "EasyTree", -1),
                new GEDCOMAppFormat(GEDCOMFormat.gf_FamilyHistorian, "FAMILY_HISTORIAN", "Family Historian", -1),
                new GEDCOMAppFormat(GEDCOMFormat.gf_FamilyTreeMaker, "FTM", "Family Tree Maker", -1),
                new GEDCOMAppFormat(GEDCOMFormat.gf_FamilyTreeMaker, "FTW", "Family Tree Maker", -1),
                new GEDCOMAppFormat(GEDCOMFormat.gf_FTB, "MYHERITAGE", "MyHeritage Family Tree Builder", -1),
                new GEDCOMAppFormat(GEDCOMFormat.gf_GENBOX, "GENBOX", "Genbox Family History", -1),
                new GEDCOMAppFormat(GEDCOMFormat.gf_GENJ, "GENJ", "GENJ", -1),
                new GEDCOMAppFormat(GEDCOMFormat.gf_Geni, "Geni.com", "Geni", -1),
                new GEDCOMAppFormat(GEDCOMFormat.gf_GeneWeb, "GeneWeb", "GeneWeb", 1252),
                new GEDCOMAppFormat(GEDCOMFormat.gf_Genney, "Genney", "Genney", -1),
                new GEDCOMAppFormat(GEDCOMFormat.gf_GenoPro, "GenoPro", "GenoPro", -1),
                new GEDCOMAppFormat(GEDCOMFormat.gf_Gramps, "Gramps", "Gramps", -1),
                new GEDCOMAppFormat(GEDCOMFormat.gf_Heredis, "HEREDIS 12 PC", "Heredis", -1),
                new GEDCOMAppFormat(GEDCOMFormat.gf_Legacy, "Legacy", "Legacy", -1),
                new GEDCOMAppFormat(GEDCOMFormat.gf_Lifelines, "Lifelines", "Lifelines", -1),
                new GEDCOMAppFormat(GEDCOMFormat.gf_PAF, "PAF", "Personal Ancestral File", -1),
                new GEDCOMAppFormat(GEDCOMFormat.gf_Reunion, "Reunion", "Reunion", -1),
                new GEDCOMAppFormat(GEDCOMFormat.gf_RootsMagic, "RootsMagic", "RootsMagic", -1),
            };

            GEDCOMFactory f = GEDCOMFactory.GetInstance();

            f.RegisterTag(GEDCOMTagType.ADDR, GDMAddress.Create, AddAddressTag, WriteAddress, true, false);
            f.RegisterTag(GEDCOMTagType.ADOP, GDMIndividualEvent.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.AGNC, null, null, null, true, false);
            f.RegisterTag(GEDCOMTagType.ALIA, GDMAlias.Create, AddBaseTag, null, true, false);
            f.RegisterTag(GEDCOMTagType.ANCI, GDMPointer.Create, AddBaseTag);
            f.RegisterTag(GEDCOMTagType.ANUL, GDMFamilyEvent.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.ASSO, null, AddAssociationTag, WriteAssociation, true, false);
            f.RegisterTag(GEDCOMTagType.AUTH, null, null, null, true, false);

            f.RegisterTag(GEDCOMTagType.BAPM, GDMIndividualEvent.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.BARM, GDMIndividualEvent.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.BASM, GDMIndividualEvent.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.BIRT, GDMIndividualEvent.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.BLES, GDMIndividualEvent.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.BURI, GDMIndividualEvent.Create, AddCustomEventTag, WriteCustomEvent);

            f.RegisterTag(GEDCOMTagType.CAST, GDMIndividualAttribute.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.CAUS, null, null, null, true, false);
            f.RegisterTag(GEDCOMTagType.CHAN, GDMChangeDate.Create, AddChangeDateTag, WriteChangeDate, true, false);
            f.RegisterTag(GEDCOMTagType.CHR, GDMIndividualEvent.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.CHRA, GDMIndividualEvent.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.CITY, null, null, null, true, false);
            f.RegisterTag(GEDCOMTagType.CONF, GDMIndividualEvent.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.CREM, GDMIndividualEvent.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.CTRY, null, null, null, true, false);

            f.RegisterTag(GEDCOMTagType.DATA, null, null, null, true, false);
            f.RegisterTag(GEDCOMTagType.DATE, GDMDateValue.Create, AddBaseTag, WriteBaseTag, true, false);
            f.RegisterTag(GEDCOMTagType.DEAT, GDMIndividualEvent.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.DESI, GDMPointer.Create, AddBaseTag);
            f.RegisterTag(GEDCOMTagType.DIV, GDMFamilyEvent.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.DIVF, GDMFamilyEvent.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.DSCR, GDMIndividualAttribute.Create, AddCustomEventTag, WriteCustomEvent);

            f.RegisterTag(GEDCOMTagType.EDUC, GDMIndividualAttribute.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.EMIG, GDMIndividualEvent.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.ENGA, GDMFamilyEvent.Create, AddCustomEventTag, WriteCustomEvent);

            f.RegisterTag(GEDCOMTagType.FACT, GDMIndividualAttribute.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.FAMC, GDMChildToFamilyLink.Create, AddChildToFamilyLinkTag, WriteChildToFamilyLink);
            f.RegisterTag(GEDCOMTagType.FAMS, GDMSpouseToFamilyLink.Create, AddPointerWithNotesTag);
            f.RegisterTag(GEDCOMTagType.FCOM, GDMIndividualEvent.Create, AddCustomEventTag, WriteCustomEvent);

            f.RegisterTag(GEDCOMTagType.GIVN, null, null, null, true, false);
            f.RegisterTag(GEDCOMTagType.GRAD, GDMIndividualEvent.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.HUSB, null, null, null, true, false);
            f.RegisterTag(GEDCOMTagType.IDNO, GDMIndividualAttribute.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.IMMI, GDMIndividualEvent.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.LANG, GDMLanguage.Create, AddBaseTag, WriteBaseTag, true, false);

            f.RegisterTag(GEDCOMTagType.MAP, GDMMap.Create, AddMapTag, WriteMap, true, false);
            f.RegisterTag(GEDCOMTagType.MARB, GDMFamilyEvent.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.MARC, GDMFamilyEvent.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.MARL, GDMFamilyEvent.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.MARR, GDMFamilyEvent.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.MARS, GDMFamilyEvent.Create, AddCustomEventTag, WriteCustomEvent);

            f.RegisterTag(GEDCOMTagType.NATI, GDMIndividualAttribute.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.NATU, GDMIndividualEvent.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.NCHI, GDMIndividualAttribute.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.NICK, null, null, null, true, false);
            f.RegisterTag(GEDCOMTagType.NMR, GDMIndividualAttribute.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.NPFX, null, null, null, true, false);
            f.RegisterTag(GEDCOMTagType.NSFX, null, null, null, true, false);

            f.RegisterTag(GEDCOMTagType.OCCU, GDMIndividualAttribute.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.ORDN, GDMIndividualEvent.Create, AddCustomEventTag, WriteCustomEvent);

            f.RegisterTag(GEDCOMTagType.PAGE, null, null, null, true, false);
            f.RegisterTag(GEDCOMTagType.PHON, null, null, null, true, false);
            f.RegisterTag(GEDCOMTagType.PLAC, GDMPlace.Create, AddPlaceTag, WritePlace, true, false);
            f.RegisterTag(GEDCOMTagType.POST, null, null, null, true, false);
            f.RegisterTag(GEDCOMTagType.PROB, GDMIndividualEvent.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.PROP, GDMIndividualAttribute.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.PUBL, null, null, null, true, false);

            f.RegisterTag(GEDCOMTagType.REFN, GDMUserReference.Create, AddUserReferenceTag, WriteUserReference);
            f.RegisterTag(GEDCOMTagType.RELI, GDMIndividualAttribute.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.RESN, null, null, null, true, false);
            f.RegisterTag(GEDCOMTagType.RETI, GDMIndividualEvent.Create, AddCustomEventTag, WriteCustomEvent);

            f.RegisterTag(GEDCOMTagType.SPFX, null, null, null, true, false);
            f.RegisterTag(GEDCOMTagType.SSN, GDMIndividualAttribute.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.STAE, null, null, null, true, false);
            f.RegisterTag(GEDCOMTagType.SUBM, null, null, null, true, false);
            f.RegisterTag(GEDCOMTagType.SUBN, null, null, null, true, false);
            f.RegisterTag(GEDCOMTagType.SURN, null, null, null, true, false);

            f.RegisterTag(GEDCOMTagType.TEXT, null, null, null, true, false);
            f.RegisterTag(GEDCOMTagType.TIME, GDMTime.Create, AddBaseTag, WriteBaseTag, true, false);
            f.RegisterTag(GEDCOMTagType.TITL, GDMIndividualAttribute.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.TYPE, null, null, null, true, false);

            f.RegisterTag(GEDCOMTagType.VERS, null, null, null, true, false);
            f.RegisterTag(GEDCOMTagType.WIFE, null, null, null, true, false);
            f.RegisterTag(GEDCOMTagType.WILL, GDMIndividualEvent.Create, AddCustomEventTag, WriteCustomEvent);

            // extensions
            f.RegisterTag(GEDCOMTagType._AWARD, GDMIndividualAttribute.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType._BGRO, GDMIndividualAttribute.Create, AddCustomEventTag, WriteCustomEvent, true, true);
            f.RegisterTag(GEDCOMTagType._CENN, null, null, null, true, true);
            f.RegisterTag(GEDCOMTagType._EYES, GDMIndividualAttribute.Create, AddCustomEventTag, WriteCustomEvent, true, true);
            f.RegisterTag(GEDCOMTagType._HAIR, GDMIndividualAttribute.Create, AddCustomEventTag, WriteCustomEvent, true, true);
            f.RegisterTag(GEDCOMTagType._HOBBY, GDMIndividualAttribute.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType._LANG, GDMLanguage.Create, AddBaseTag, WriteBaseTag, true, true);
            f.RegisterTag(GEDCOMTagType._LOC, GDMPointer.Create, AddBaseTag, WriteBaseTag, true, true);
            f.RegisterTag(GEDCOMTagType._MARN, null, null, null, true, true);
            f.RegisterTag(GEDCOMTagType._MDNA, GDMIndividualAttribute.Create, AddCustomEventTag, WriteCustomEvent, true, true);
            f.RegisterTag(GEDCOMTagType._MILI, GDMIndividualAttribute.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType._MILI_DIS, GDMIndividualAttribute.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType._MILI_IND, GDMIndividualAttribute.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType._MILI_RANK, GDMIndividualAttribute.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType._PATN, null, null, null, true, true);
            f.RegisterTag(GEDCOMTagType._POSITION, GDMCutoutPosition.Create, AddBaseTag, WriteBaseTag, true, true);
            f.RegisterTag(GEDCOMTagType._RELN, null, null, null, true, true);
            f.RegisterTag(GEDCOMTagType._TRAVEL, GDMIndividualAttribute.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType._YDNA, GDMIndividualAttribute.Create, AddCustomEventTag, WriteCustomEvent, true, true);

            f.RegisterTag(GEDCOMTagType.NAME, GDMPersonalName.Create, AddPersonalNameTag, WritePersonalName); // INDI.NAME!

            // FIXME
            // indi
            f.RegisterTag(GEDCOMTagType.CENS, GDMIndividualEvent.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.EVEN, GDMIndividualEvent.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.RESI, GDMIndividualAttribute.Create, AddCustomEventTag, WriteCustomEvent);

            // fam records
            f.RegisterTag(GEDCOMTagType.CENS, GDMFamilyEvent.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.EVEN, GDMFamilyEvent.Create, AddCustomEventTag, WriteCustomEvent);
            f.RegisterTag(GEDCOMTagType.RESI, GDMFamilyEvent.Create, AddCustomEventTag, WriteCustomEvent);

            // TODO
            //f.RegisterTag("_OBIT", true,  true); // Obituary
            //f.RegisterTag("_ELEC", true,  true); // Election
            //f.RegisterTag("_MDCL", true,  true); // Medical condition
            //f.RegisterTag("_EXCM", true,  true); // Excommunication!
        }

        public static bool SkipEmptyTag(string tagName)
        {
            if (string.IsNullOrEmpty(tagName)) return false;

            TagInfo props = GEDCOMFactory.GetInstance().GetTagInfo(tagName);
            return (props != null && props.SkipEmpty);
        }

        #endregion
    }
}

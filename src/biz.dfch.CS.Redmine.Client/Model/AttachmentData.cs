/**
 * Copyright 2015 d-fens GmbH
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
 
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace biz.dfch.CS.Redmine.Client.Model
{
    public class AttachmentData
    {
        public const string FileNameKey = "FileName";
        public const string DescriptionKey = "Description";
        public const string ContentTypeKey = "ContentType";
        public const string ContentKey = "Content";
        public const string NotesKey = "Notes";
        public const string PrivateNotesKey = "PrivateNotes";

        public string FileName { get; set; }
        public string Description { get; set; }
        public string ContentType { get; set; }
        public byte[] Content { get; set; }
        public string Notes { get; set; }
        public bool PrivateNotes { get; set; }

        public AttachmentData()
        {
        }

        public AttachmentData(Dictionary<string, object> values)
        {
            this.FileName = Util.GetValue<string>(values, AttachmentData.FileNameKey);
            this.Description = Util.GetValue<string>(values, AttachmentData.DescriptionKey);
            this.ContentType = Util.GetValue<string>(values, AttachmentData.ContentTypeKey);
            this.Content = Util.GetValue<byte[]>(values, AttachmentData.ContentKey);
            this.Notes = Util.GetValue<string>(values, AttachmentData.NotesKey);
            this.PrivateNotes = Util.GetValue<bool>(values, AttachmentData.PrivateNotesKey);
        }
    }
}

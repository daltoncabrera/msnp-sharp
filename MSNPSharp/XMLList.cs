﻿#define TRACE

namespace MSNPSharp
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Xml;
    using System.IO;
    using MSNPSharp.IO;

    internal enum XMLContactListTags
    {
        ContactList,
        MembershipList,
        AddressBook,
        Members,
        Member,
        groups,
        Group,
        contacts,
        Contact,
        Settings,
        contactType,
        Name,
        Value,
        Me,
        Annotations,
        Annotation,
        Service,
        Membership
    }

    internal abstract class XMLContactList : Dictionary<string, ContactInfo>
    {
        protected bool noCompress;
        protected XmlDocument doc;
        protected DateTime lastChange;

        protected XMLContactList(bool nocompress)
            : base(0)
        {
            noCompress = nocompress;
        }

        public DateTime LastChange
        {
            get
            {
                return lastChange;
            }
            set
            {
                lastChange = value;
            }
        }

        protected void CreateDoc()
        {
            doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
            XmlNode root = CreateNode(XMLContactListTags.ContactList.ToString(), null);
            doc.AppendChild(root);
            root.AppendChild(CreateNode(XMLContactListTags.MembershipList.ToString(), null));
            root.AppendChild(CreateNode(XMLContactListTags.AddressBook.ToString(), null));
        }

        protected virtual XmlNode CreateNode(string name, string innerText)
        {
            if (null == doc)
                CreateDoc();

            XmlNode rtn = doc.CreateElement(name);
            rtn.InnerText = innerText;
            return rtn;
        }

        public abstract void Save(string filename);
        public abstract void Save();

        protected virtual void SaveToHiddenMCL(string filename)
        {
            MemoryStream ms = new MemoryStream();
            doc.Save(ms);
            MCLFile file = MCLFileManager.GetFile(filename, noCompress);
            file.Content = ms.ToArray();
            MCLFileManager.Save(file, true);
        }

        public virtual void LoadFromFile(string filename)
        {
            if (File.Exists(filename))
            {
                CreateDoc();
                MCLFile file = MCLFileManager.GetFile(filename, noCompress);
                if (file.Content != null)
                {
                    doc.Load(new MemoryStream(file.Content));
                }
            }
            else
            {
                CreateDoc();
                SaveToHiddenMCL(filename);
            }
        }

        public virtual void Add(Dictionary<string, ContactInfo> range)
        {
            foreach (string account in range.Keys)
            {
                if (ContainsKey(account))
                {
                    if (this[account].LastChanged.CompareTo(range[account].LastChanged) <= 0)
                    {
                        this[account] = range[account];
                    }
                }
                else
                {
                    Add(account, range[account]);
                }
            }
        }
    }
};

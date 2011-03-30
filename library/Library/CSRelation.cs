#region License
//=============================================================================
// Vici CoolStorage - .NET Object Relational Mapping Library 
//
// Copyright (c) 2004-2009 Philippe Leybaert
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal 
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//=============================================================================
#endregion

using System;

namespace Vici.CoolStorage
{
	internal enum CSSchemaRelationType
	{
		None ,
		OneToMany ,
		ManyToOne,
		OneToOne,
		ManyToMany
	}

	internal class CSRelation
	{
        private readonly CSSchema _schema;

		internal RelationAttribute Attribute;
        
		internal string LocalKey;
		internal string ForeignKey;

		internal string LocalLinkKey;   // for many-to-many relations
		internal string ForeignLinkKey; // for many-to-many relations
		internal string LinkTable;      // for many-to-many relations
		internal bool   PureManyToMany;

		internal Type ForeignType;

		internal CSSchemaRelationType RelationType = CSSchemaRelationType.None;

        internal CSSchema Schema
        {
            get
            {
                return _schema;
            }
        }

		internal CSRelation(CSSchema schema, RelationAttribute att)
		{
            _schema = schema;

			Attribute = att;
		}

		internal CSSchema ForeignSchema
		{
			get
			{
				return CSSchema.Get(ForeignType);
			}
		}
	}
}

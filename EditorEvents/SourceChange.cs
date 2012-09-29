/*
 * Created by SharpDevelop.
 * User: Honza
 * Date: 18.9.2011
 * Time: 0:46
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Security;

using ICSharpCode.SharpDevelop.Dom;

namespace EnC.EditorEvents
{
	/// <summary>
	/// Holds information about one change in sourcecode or project structure.
	/// </summary>
	public class SourceChange
	{
		/// <summary>
		/// Kind of member changed by this change.
		/// </summary>
		public SourceChangeMember MemberKind{
			get;private set;
		}
		/// <summary>
		/// Action done to member.
		/// </summary>
		public SourceChangeAction MemberAction{
			get;private set;
		}
		/// <summary>
		/// Was IP inside body when changing. 
		/// Only for methods and properties.
		/// </summary>
		public bool IsIPInside{
			get; set;
		}
		/// <summary>
		/// Old version of IEntity, if there is any.
		/// </summary>
		public IEntity OldEntity{
			get; private set;
		}
		/// <summary>
		/// New version of IEntity, if there is any.
		/// </summary>
		public IEntity NewEntity{
			get; private set;
		}
		/// <summary>
		/// Creates SourceChange
		/// </summary>
		/// <param name="oldEntity">Old version of IEntity.</param>
		/// <param name="newEntity">New version of IEntity.</param>
		/// <param name="memberKind">Kind of member changed.</param>
		/// <param name="action">Action done to member.</param>
		public SourceChange(IEntity oldEntity,IEntity newEntity, SourceChangeMember memberKind, SourceChangeAction action, bool isError = false)
		{
			OldEntity = oldEntity;
			NewEntity = newEntity;
			MemberAction = action;
			MemberKind = memberKind;
            IsError = isError;
		}
        /// <summary>
        /// If it's not EnC valid.
        /// </summary>
        public bool IsError
        {
            get;
            private set;
        }
	}
    /// <summary>
    /// Enum describing kinds of language struct.
    /// </summary>
	public enum SourceChangeMember{
		Method,
		Property,
		Class,
		Field,
		Event
	}
    /// <summary>
    /// Enum describing edit action done to entity.
    /// </summary>
	public enum SourceChangeAction{
		Created,
		ModifierChanged,
		TypeParameterChanged,
		BodyChanged,
		Removed,
        ReturnTypeChanged
	}
    /// <summary>
    /// Special kind of SourceChange, representing change to body of method or property.
    /// </summary>
    public class BodyChange : SourceChange
    {
        public BodyChange(IEntity oldEntity, IEntity newEntity, SourceChangeMember memberKind, SourceChangeAction action, bool isGetter, bool isError = false)
            :base(oldEntity,newEntity, memberKind, action, isError)
		{
            this.isGetter = isGetter;
		}
        public bool isGetter;
    }
}

/*
 * Created by SharpDevelop.
 * User: Honza
 * Date: 1.11.2011
 * Time: 14:25
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using ICSharpCode.SharpDevelop.Dom;

namespace EnC.EditorEvents
{
	/// <summary>
	/// Holds copy of project structure, can search for differncies between other project.
	/// </summary>
	public class ProjectContentCopy : DefaultProjectContent{
        /// <summary>
        /// <c>List</c> of classes contained in project.
        /// </summary>
		List<IClass> classes = new List<IClass>();
		
		public ProjectContentCopy(IProjectContent cont)
		{
			foreach (IClass element in cont.Classes) {
				IClass newClass = copyClass(element);
				this.AddClassToNamespaceListInternal(newClass);
				classes.Add(newClass);
				int count = this.Classes.Count;
			}
		}
		/// <summary>
		/// Checks whether entity exists in state copy.
		/// </summary>
		/// <param name="ent">Entity to be looked for.</param>
		public bool Exist(IEntity ent)
		{
			IClass lClass = this.GetClass(ent.DeclaringType.FullyQualifiedName,ent.DeclaringType.TypeParameters.Count);
            if (lClass == null)
                return false;
			if(ent is IMethod){
				return (findMethod(lClass,(IMethod) ent) != null);
			} else if(ent is IProperty){
				return (findProperty(lClass,(IProperty) ent) != null);
			} else {
				return false;
			}
		}
		/// <summary>
		/// Finds changes of particular members in project, which where made from this 
		/// version to version given as cont.
		/// </summary>
		/// <param name="cont">IProjectContent to which is this project content compared.</param>
		/// <param name="edEvent">EditorEvent representing changes by user in text editor.</param>
		/// <returns>List of SourceChange representing changes done to project</returns>
		public List<SourceChange> DiffTo(IProjectContent cont)
		{
			List<SourceChange> changes = new List<SourceChange>();
            diffToClasses(this.classes, cont.Classes, changes);
			return changes;
		}
        /// <summary>
        /// Find differences between list of classes.
        /// </summary>
        /// <param name="classes1">First list of classes.</param>
        /// <param name="classes2">Seconf list of classes.</param>
        /// <param name="changes"><c>List</c> containing changes in classes.</param>
        private void diffToClasses(IEnumerable<IClass> classes1, IEnumerable<IClass> classes2,List<SourceChange> changes)
        {
            foreach (IClass element in classes1) {
                IClass gClass = ProjectContentCopy.GetClass(classes2, element.FullyQualifiedName, element.TypeParameters.Count);
                if (gClass != null) {

                    // For classes in both this.classes and cont
                    diffToClass(element, gClass, changes);
                } else {

                    // For classes in this.classes and not in cont.
                    SourceChange classRemoved = new SourceChange(element, element, SourceChangeMember.Class, SourceChangeAction.Removed);
                    changes.Add(classRemoved);
                }
            }
            // For classes in cont and not in this.classes.
            foreach (IClass element in classes2) {
                IClass gClass = ProjectContentCopy.GetClass(classes2, element.FullyQualifiedName, element.TypeParameters.Count);
                if (gClass == null) {
                    SourceChange classRemoved = new SourceChange(element, element, SourceChangeMember.Class, SourceChangeAction.Created);
                    changes.Add(classRemoved);
                }
            }
        }
        /// <summary>
        /// Returns class by <c>fullyQualifiedName</c> and <c>typeParameterCount</c>.
        /// </summary>
		public new IClass GetClass(string fullyQualifiedName, int typeParameterCount)
		{
			return ProjectContentCopy.GetClass(this,fullyQualifiedName,typeParameterCount);
		}
        /// <summary>
        /// Returns class from <c>IProjectContent cont</c> by <c>fullyQualifiedName</c> and <c>typeParameterCount</c>.
        /// </summary>
        public static IClass GetClass(IProjectContent cont, string fullyQualifiedName, int typeParameterCount)
        {
            return GetClass(cont.Classes, fullyQualifiedName, typeParameterCount);
        }
        /// <summary>
        /// Returns class from <c>classes</c> by <c>fullyQualifiedName</c> and <c>typeParameterCount</c>.
        /// </summary>
		public static IClass GetClass(IEnumerable<IClass> classes, string fullyQualifiedName, int typeParameterCount)
		{
			foreach (IClass element in classes) {
				if(element.FullyQualifiedName == fullyQualifiedName && element.TypeParameters.Count == typeParameterCount){
					return element;
				}
			}
			return null;
		}
		/// <summary>
		/// Makes a copy of <c>oldClass</c>.
		/// </summary>
		/// <param name="oldClass">Class to be copied.</param>
		/// <returns>Copy of the class.</returns>
		private IClass copyClass(IClass oldClass)
		{
			DefaultClass newClass = new DefaultClass(oldClass.CompilationUnit,oldClass.FullyQualifiedName);
			newClass.Modifiers = oldClass.Modifiers;
			foreach (ITypeParameter element in oldClass.TypeParameters) {
				newClass.TypeParameters.Add(element);
			}
			// Recursively copy inner classes.
			foreach(IClass element in oldClass.InnerClasses){
				newClass.InnerClasses.Add(copyClass(element));
			}
			
			// Copy events.
			foreach(IEvent element in oldClass.Events){
				DefaultEvent newEvent = new DefaultEvent(element.Name,element.ReturnType,element.Modifiers,
				                                        element.Region,element.BodyRegion,newClass);
				newClass.Events.Add(newEvent);
			}
			
			//Copy properties
			foreach(IProperty element in oldClass.Properties){
				DefaultProperty newProperty = new DefaultProperty(element.Name,element.ReturnType,
		                                                  element.Modifiers,element.Region,element.BodyRegion,newClass);
				newClass.Properties.Add(newProperty);				
			}
			
			//Copy methods.
			
			copyMethods(oldClass,newClass);
			copyFields(oldClass,newClass);
			
			return newClass;
		}
		/// <summary>
		/// Copies fields from one class to another.
		/// </summary>
		/// <param name="oldClass">Source class</param>
		/// <param name="newClass">Target class</param>
		private void copyFields(IClass oldClass, IClass newClass)
		{
			foreach(IField element in oldClass.Fields){
				DefaultField newField = new DefaultField(element.ReturnType,element.Name,element.Modifiers,element.Region,newClass);
				newClass.Fields.Add(newField);
			}
		}
        /// <summary>
        /// Copies methods from one class to another.
        /// </summary>
        /// <param name="oldClass">Source class</param>
        /// <param name="newClass">Target class</param>		
		private void copyMethods(IClass oldClass, IClass newClass)
		{
			foreach(IMethod element in oldClass.Methods){
				DefaultMethod newMethod = new DefaultMethod(element.Name,element.ReturnType,
				                              				element.Modifiers,element.Region,element.BodyRegion,newClass);
				foreach(IParameter param in element.Parameters){
					DefaultParameter newParam = new DefaultParameter(param);
					newMethod.Parameters.Add(newParam);
				}
				newMethod.BodyRegion = new DomRegion(element.BodyRegion.BeginLine,element.BodyRegion.BeginColumn,
				                                     element.BodyRegion.EndLine,element.BodyRegion.EndColumn);
				newClass.Methods.Add(newMethod);
			}
		}
        /// <summary>
        /// Find changes between two classes.
        /// </summary>
        /// <param name="oldClass">First class</param>
        /// <param name="newClass">Second class</param>
        /// <param name="changes"><c>List</c> of SourceChange, where new changes are added.</param>
		private void diffToClass(IClass oldClass,IClass newClass,List<SourceChange> changes)
		{
			if(oldClass.Modifiers.CompareTo(newClass.Modifiers) != 0){
				changes.Add(new SourceChange(oldClass,newClass, SourceChangeMember.Class, SourceChangeAction.ModifierChanged));
			}
			
			foreach (ITypeParameter element in oldClass.TypeParameters) {
				if(element.Index < newClass.TypeParameters.Count){
					if(newClass.TypeParameters[element.Index].Name != element.Name){
						changes.Add(new SourceChange(oldClass,newClass, SourceChangeMember.Class, SourceChangeAction.TypeParameterChanged));
					}
				} else {
					changes.Add(new SourceChange(oldClass,newClass, SourceChangeMember.Class, SourceChangeAction.TypeParameterChanged));
				}
			}
			
			// In case the parameters where added at end
			foreach (ITypeParameter element in newClass.TypeParameters) {
				if(element.Index < oldClass.TypeParameters.Count){
					if(oldClass.TypeParameters[element.Index].Name != element.Name){
						changes.Add(new SourceChange(oldClass,newClass, SourceChangeMember.Class, SourceChangeAction.TypeParameterChanged));
					}
				} else {
					changes.Add(new SourceChange(oldClass,newClass, SourceChangeMember.Class, SourceChangeAction.TypeParameterChanged));
				}
			}
			
			// Diff events.
			foreach(IEvent element in oldClass.Events){
                IEvent evnt = findEvent(newClass, element);
                if (evnt == null) {
                    changes.Add(new SourceChange(element, element, SourceChangeMember.Event, SourceChangeAction.Removed));
                } else if (element.Modifiers.CompareTo(evnt.Modifiers) != 0) {
                    changes.Add(new SourceChange(element,evnt,SourceChangeMember.Event,SourceChangeAction.ModifierChanged));
                } else if (element.ReturnType.FullyQualifiedName != evnt.ReturnType.FullyQualifiedName){
                    changes.Add(new SourceChange(element,evnt,SourceChangeMember.Event,SourceChangeAction.BodyChanged));
                }
			}
            foreach (IEvent element in newClass.Events) {
                IEvent evnt = findEvent(oldClass, element);
                if (evnt == null) {
                    changes.Add(new SourceChange(element, element, SourceChangeMember.Event, SourceChangeAction.Created));
                }
            }
            
            diffToClasses(oldClass.InnerClasses, newClass.InnerClasses,changes);
            diffToFields(oldClass, newClass, changes);	
			diffToProperties(oldClass,newClass,changes);
			diffToMethods(oldClass,newClass,changes);
		}
        /// <summary>
        /// Find changes between properties of two classes.
        /// </summary>
        /// <param name="oldClass">First class</param>
        /// <param name="newClass">Second class</param>
        /// <param name="changes"><c>List</c> of SourceChange, where new changes are added.</param>
		private void diffToProperties(IClass oldClass, IClass newClass, List<SourceChange> changes)
		{
			// Check old properties
			foreach(IProperty element in oldClass.Properties){
				IProperty found = findProperty(newClass,element);
				if(found == null){
                    changes.Add(new SourceChange(element, element, SourceChangeMember.Property, SourceChangeAction.Removed));
				} else {
					if(found.Modifiers.CompareTo(element.Modifiers) != 0){
						changes.Add(new SourceChange(element, found, SourceChangeMember.Property, SourceChangeAction.ModifierChanged));
					} else if(found.DeclaringType.FullyQualifiedName != found.DeclaringType.FullyQualifiedName) {
						changes.Add(new SourceChange(element, found, SourceChangeMember.Property, SourceChangeAction.ReturnTypeChanged));
					}
				}
			}
			// Look for new properties
			foreach(IProperty element in newClass.Properties){
				IProperty found = findProperty(oldClass,element);
				if(found == null){
					changes.Add(new SourceChange(element, element, SourceChangeMember.Property, SourceChangeAction.Created));					
				}
			}
		}

        /// <summary>
        /// Find changes between methods of two classes.
        /// </summary>
        /// <param name="oldClass">First class</param>
        /// <param name="newClass">Second class</param>
        /// <param name="changes"><c>List</c> of SourceChange, where new changes are added.</param>
		private void diffToMethods(IClass oldClass, IClass newClass, List<SourceChange> changes)
		{
			//Check old methods
			foreach(IMethod element in oldClass.Methods){
				IMethod found = findMethod(newClass,element);
				if(found == null){
					changes.Add(new SourceChange(null,element, SourceChangeMember.Method, SourceChangeAction.Removed));
				} else {
					if(found.Modifiers.CompareTo(element.Modifiers) != 0){
						changes.Add(new SourceChange(element,found, SourceChangeMember.Method, SourceChangeAction.ModifierChanged));
                    } else if (found.ReturnType.FullyQualifiedName != element.ReturnType.FullyQualifiedName) {
                        changes.Add(new SourceChange(element, found, SourceChangeMember.Method, SourceChangeAction.ReturnTypeChanged));
					}
				}
			}
			
			// Look for new methods
			foreach(IMethod element in newClass.Methods){
				IMethod found = findMethod(oldClass,element);
				if(found == null){
					changes.Add(new SourceChange(null,element, SourceChangeMember.Method, SourceChangeAction.Created));					
				}
			}
		}

        /// <summary>
        /// Find changes between fields of two classes.
        /// </summary>
        /// <param name="oldClass">First class</param>
        /// <param name="newClass">Second class</param>
        /// <param name="changes"><c>List</c> of SourceChange, where new changes are added.</param>
		private void diffToFields(IClass oldClass, IClass newClass, List<SourceChange> changes)
		{			
			//Check old fields
			foreach (IField element in oldClass.Fields) {
                IField found = null;
                foreach (IField element2 in newClass.Fields) {
                    if (element2.Name == element.Name) {
                        found = element2;
                    }
                }
                if (found != null) {
                    if (found.Modifiers.CompareTo(element.Modifiers) != 0) {
                        changes.Add(new SourceChange(element, element, SourceChangeMember.Field, SourceChangeAction.ModifierChanged));
                    } else if (found.DeclaringType.FullyQualifiedName != element.DeclaringType.FullyQualifiedName) {
                        changes.Add(new SourceChange(element, element, SourceChangeMember.Field, SourceChangeAction.ReturnTypeChanged));
                    }
                } else {
                    changes.Add(new SourceChange(element,element,SourceChangeMember.Field,SourceChangeAction.Removed));
                }
			}
				
			//Look for new fields
			foreach (IField element in newClass.Fields) {
				IField found = null;
                foreach (IField element2 in newClass.Fields) {
                    if (element2.Name == element.Name) {
                        found = element2;
                    }
                }
                if (found == null) {
                    changes.Add(new SourceChange(element, element, SourceChangeMember.Field, SourceChangeAction.Created));
                }
			}
		}
        /// <summary>
        /// Finds <c>IProperty</c> in <c>inClass</c> instance representing same property, but other version of <c>property</c>.
        /// </summary>
        /// <param name="inClass">Class where other version of property should be found.</param>
        /// <param name="property">Property to which other version is to be found.</param>
		private IProperty findProperty(IClass inClass, IProperty property)
		{
			foreach(IProperty elProperty in inClass.Properties){
				if(MemberComparator.SameProperties(property,elProperty)){
					return elProperty;
				}
			}
			return null;
		}
        /// <summary>
        /// Finds <c>IMethod</c> in <c>inClass</c> instance representing same method overload, but other version of <c>method</c>.
        /// </summary>
        /// <param name="inClass">Class where other version of method should be found.</param>
        /// <param name="property">Method to which other version is to be found.</param>
		private IMethod findMethod(IClass inClass, IMethod method)
		{
			foreach(IMethod elMethod in inClass.Methods){
				if(MemberComparator.SameMethodOverloads(elMethod,method)){
					return elMethod;
				}
			}
			return null;
		}

        /// <summary>
        /// Finds <c>IEvent</c> in <c>inClass</c> instance representing same event, but other version of <c>pEvent</c>.
        /// </summary>
        /// <param name="inClass">Class where other version of event should be found.</param>
        /// <param name="property">Event to which other version is to be found.</param>
        private IEvent findEvent(IClass inClass,IEvent pEvent)
        {
            foreach (IEvent elEvent in inClass.Methods) {
                if (elEvent.Name == pEvent.Name) {
                    return elEvent;
                }
            }
            return null;
        }
	}
}

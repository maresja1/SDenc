/*
 * Created by SharpDevelop.
 * User: Honza
 * Date: 12.12.2011
 * Time: 15:06
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using ICSharpCode.SharpDevelop.Dom;
using ICSharpCode.SharpDevelop.Project;

namespace EnC.EditorEvents
{
	/// <summary>
	/// Class for validation of source code changes, whether they are EnC valid changes. 
    /// If not, there is possibility to get some error description.
	/// </summary>
	public static class SourceChangeValidator
	{
		/// <summary>
		/// Checks whether is source change EnC-valid, if so, returns null. If not, returns BuildError instance.
		/// </summary>
		/// <param name="change">SourceChange instance to be checked for validity.</param>
		/// <returns>Null or BuildError instance in case of not EnC valid change given.</returns>
		public static BuildError IsValid(SourceChange change)
        {
            if (change.IsError) {
                return createError(change.OldEntity, "Element cannot be changed by EnC.");
            }
			switch (change.MemberAction) {
				case SourceChangeAction.Created:
					return isValidCreation(change.NewEntity);
				case SourceChangeAction.ModifierChanged:
					return createError(change.OldEntity,"Cannot modify entities's definition with EnC.");
				case SourceChangeAction.TypeParameterChanged:
					return createError(change.OldEntity,"Cannot modify entities's definition with EnC.");
				case SourceChangeAction.BodyChanged:
					return null;
				case SourceChangeAction.Removed:
					return createError(change.OldEntity,"Cannot remove entities with EnC.");
                case SourceChangeAction.ReturnTypeChanged:
                    return createError(change.OldEntity, "Cannot change type of entity or return type with EnC.");
				default:
					return new BuildError("","Cannot reckognize change.");
			}
		}
        /// <summary>
        /// Checks whether creation of some entity is EnC valid.
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <returns>Error if there is any, otherwise <c>null</c>.</returns>
		private static BuildError isValidCreation(IEntity entity)
		{
			switch (entity.EntityType) {
				case EntityType.Class:
					return createError(entity,"Cannot create classes with EnC.");
				case EntityType.Field:
					return (entity.IsPrivate ? null : createError(entity,"Field must be private."));
				case EntityType.Property:
					return (entity.IsPrivate ? null : createError(entity,"Property must be private."));
				case EntityType.Method:
					return (entity.IsPrivate && !entity.IsVirtual ? null : createError(entity,"Method must be private and non-virtual."));
				case EntityType.Event:
					return createError(entity,"Cannot create events with EnC.");
				default:
					return createError(entity,"Cannot reckognize type.");
			}
		}
        /// <summary>
        /// Creates error for TaskView built from entity and errorText.
        /// </summary>
        /// <param name="entity">Entity that error concerns.</param>
        /// <param name="errorText">Text describing kind of error.</param>
        /// <returns>Instance of <c>BuildError</c>.</returns>
		private static BuildError createError(IEntity entity,string errorText)
		{
			return new BuildError(entity.CompilationUnit.FileName,entity.BodyRegion.BeginLine,
			entity.BodyRegion.BeginColumn,"0","EnC: " + errorText);
		}
		/// <summary>
		/// Obtain list of errors, made by EnC not valid changes.
		/// </summary>
		/// <param name="changes">List of changes made in running code.</param>
		/// <returns>List of errors, empty if there are not any.</returns>
		public static List<BuildError> GetErrors(List<SourceChange> changes)
		{
			List<BuildError> errors = changes.ConvertAll(new Converter<SourceChange, BuildError>(IsValid));
			errors.RemoveAll(delegate(BuildError err){return err == null;});
			errors.TrimExcess();
			return errors;
		}
	}
}

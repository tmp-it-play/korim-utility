using System.Reflection;

namespace KoRimUtility.CharacterEditor
{
    internal static class LabelFields
    {
        internal static bool IsWritableString(FieldInfo field) =>
            field != null && field.IsStatic && !field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string);
    }
}

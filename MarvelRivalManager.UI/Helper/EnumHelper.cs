using MarvelRivalManager.UI.Common;
using System;
using System.Reflection;

namespace MarvelRivalManager.UI.Helper
{
    /// <summary>
    ///     This class is used to help with enums.
    /// </summary>
    public static class EnumHelper
    {
        /// <summary>
        ///     Get enum value from string value
        /// </summary>
        /// <typeparam name="EnumType">
        ///     Type of the enum
        /// </typeparam>
        /// <param name="value">
        ///     Value of the string to parse
        /// </param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">
        ///     The enum type is not an struct enum
        /// </exception>
        public static EnumType GetEnum<EnumType>(string value) where EnumType : struct => 
            typeof(EnumType).GetTypeInfo().IsEnum 
            ? Enum.Parse<EnumType>(value) 
            : throw new InvalidOperationException(Errors.INVALID_ENUM);
    }
}

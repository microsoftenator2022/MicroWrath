﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MicroWrath.Generator
{
    internal static class Constants
    {
        internal const string ConstructorNamespace = "MicroWrath.Constructors";
        internal const string ConstructClassName = "Construct";
        internal const string ConstructNewClassName = "New";
        internal static readonly string ConstructorClassFullName = $"{ConstructorNamespace}.{ConstructClassName}";
        internal const string NewBlueprintMethodName = "Blueprint";

        internal const string BlueprintsDbNamespace = "MicroWrath.BlueprintsDb";
        internal const string BlueprintsDbTypeName = "BlueprintsDb";
        internal static readonly string BlueprintsDbTypeFullName = $"{BlueprintsDbNamespace}.{BlueprintsDbTypeName}";

        internal const string AttributeFullName = "MicroWrath.Localization.LocalizedStringAttribute";
    }
}

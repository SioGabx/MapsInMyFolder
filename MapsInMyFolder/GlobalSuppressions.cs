// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0090:Utiliser 'new(...)'", Justification = "More readable to use old version", Scope = "module")]
[assembly: SuppressMessage("Style", "IDE0063:Utiliser une instruction 'using' simple", Justification = "More readable", Scope = "module")]
[assembly: SuppressMessage("Style", "IDE0042:Déconstruire la déclaration de variable", Justification = "<En attente>", Scope = "module")]
[assembly: SuppressMessage("Style", "IDE0057:Utiliser l'opérateur de plage", Justification = "More readable", Scope = "module")]
[assembly: SuppressMessage("Performance", "SYSLIB1045:Convertissez en 'GeneratedRegexAttribute'.", Justification = "Visibility, performance is not crucial", Scope = "module")]
[assembly: SuppressMessage("Roslynator", "RCS1036:Remove unnecessary blank line", Justification = "Temporaly removed to be able to see other info ", Scope = "module")]
[assembly: SuppressMessage("AsyncUsage", "AsyncFixer03:Fire-and-forget async-void methods or delegates", Scope = "module")]

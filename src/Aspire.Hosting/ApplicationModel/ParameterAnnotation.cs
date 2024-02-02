// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation for a parameter resource. Used to indicate that a doubles as a parameter resource at publish time.
/// </summary>
/// <param name="parameter">The parameter resource.</param>
public class ParameterAnnotation(ParameterResource parameter) : IResourceAnnotation
{
    public ParameterResource Parameter { get; } = parameter;
}

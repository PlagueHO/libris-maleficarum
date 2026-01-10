// <copyright file="AssemblyInfo.cs" company="Daniel Scott-Raynsford">
// Copyright (c) Daniel Scott-Raynsford. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;

// Enable test parallelization for faster unit test execution
[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]

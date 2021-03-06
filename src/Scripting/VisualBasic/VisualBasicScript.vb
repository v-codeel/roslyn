' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Scripting.VisualBasic

    ''' <summary>
    ''' A factory for creating and running Visual Basic scripts.
    ''' </summary>
    Public Module VisualBasicScript
        ''' <summary>
        ''' Create a new Visual Basic script.
        ''' </summary>
        Public Function Create(Of T)(code As String, Optional options As ScriptOptions = Nothing, Optional globalsType As Type = Nothing) As Script(Of T)
            Return New Script(Of T)(VisualBasicScriptCompiler.Instance, code, options, globalsType, Nothing, Nothing)
        End Function

        ''' <summary>
        ''' Create a new Visual Basic script.
        ''' </summary>
        Public Function Create(code As String, Optional options As ScriptOptions = Nothing, Optional globalsType As Type = Nothing) As Script(Of Object)
            Return Create(Of Object)(code, options, globalsType)
        End Function

        ''' <summary>
        ''' Run a Visual Basic script.
        ''' </summary>
        Public Function RunAsync(Of T)(code As String,
                                       Optional options As ScriptOptions = Nothing,
                                       Optional globals As Object = Nothing,
                                       Optional cancellationToken As CancellationToken = Nothing) As Task(Of ScriptState(Of T))
            Return Create(Of T)(code, options, globals?.GetType()).RunAsync(globals, cancellationToken)
        End Function

        ''' <summary>
        ''' Run a Visual Basic script.
        ''' </summary>
        Public Function RunAsync(code As String,
                                 Optional options As ScriptOptions = Nothing,
                                 Optional globals As Object = Nothing,
                                 Optional cancellationToken As CancellationToken = Nothing) As Task(Of ScriptState(Of Object))
            Return RunAsync(Of Object)(code, options, globals, cancellationToken)
        End Function

        ''' <summary>
        ''' Run a Visual Basic script and return its resulting value.
        ''' </summary>
        Public Function EvaluateAsync(Of T)(code As String,
                                            Optional options As ScriptOptions = Nothing,
                                            Optional globals As Object = Nothing,
                                            Optional cancellationToken As CancellationToken = Nothing) As Task(Of T)
            Return RunAsync(Of T)(code, options, globals, cancellationToken).GetEvaluationResultAsync()
        End Function

        ''' <summary>
        ''' Run a Visual Basic script and return its resulting value.
        ''' </summary>
        Public Function EvaluateAsync(code As String, Optional options As ScriptOptions = Nothing, Optional globals As Object = Nothing, Optional cancellationToken As CancellationToken = Nothing) As Task(Of Object)
            Return EvaluateAsync(Of Object)(code, Nothing, globals, cancellationToken)
        End Function
    End Module
End Namespace


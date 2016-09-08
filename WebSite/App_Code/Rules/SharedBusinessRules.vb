Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Linq
Imports TimberSmart.Data

Namespace TimberSmart.Rules
    
    Partial Public Class SharedBusinessRules
        Inherits TimberSmart.Data.BusinessRules
        
        Public Sub New()
            MyBase.New
        End Sub

        <ControllerAction("LoadCompleted", "grid1", "Select", ActionPhase.Before)> _
        Public Sub AssignLoadFilter()
            If Not IsTagged("Filtered") Then
                AddTag("Filtered")
                Dim endDate As DateTime = System.DateTime.Now.Date.AddDays(1)
                Dim startDate As DateTime = endDate.AddDays(-7)

                AssignFilter(
                    New FilterValue("DespatchTime",
                        RowFilterOperation.Between, startDate, endDate)
                )

            End If
        End Sub

        <ControllerAction("DespatchDocket", "grid1", "Select", ActionPhase.Before)> _
        Public Sub AssignDocketFilter()
            If Not IsTagged("Filtered") Then
                AddTag("Filtered")
                Dim endDate As DateTime = System.DateTime.Now.Date.AddDays(1)
                Dim startDate As DateTime = endDate.AddDays(-7)

                AssignFilter(
                    New FilterValue("DespatchDate",
                        RowFilterOperation.Between, startDate, endDate)
                )

            End If
        End Sub
    End Class
End Namespace

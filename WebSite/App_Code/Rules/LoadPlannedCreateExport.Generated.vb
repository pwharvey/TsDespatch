Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Linq
Imports System.Text.RegularExpressions
Imports System.Web
Imports System.Web.Security
Imports TimberSmart.Data

Namespace TimberSmart.Rules
    
    Partial Public Class LoadPlannedCreateExportBusinessRules
        Inherits TimberSmart.Rules.SharedBusinessRules
        
        <RowBuilder("LoadPlannedCreateExport", RowKind.New)>  _
        Public Sub BuildNewLoadPlannedCreateExport()
            UpdateFieldValue("Trucks", 0)
        End Sub
    End Class
End Namespace

Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Configuration
Imports System.Data
Imports System.Web
Imports System.Web.Security
Imports System.Web.UI
Imports System.Web.UI.HtmlControls
Imports System.Web.UI.WebControls
Imports System.Web.UI.WebControls.WebParts
Imports TimberSmart.Data

Namespace TimberSmart.Web
    
    Public Class MembershipManager
        Inherits Control
        Implements INamingContainer
        
        Private m_ServicePath As String
        
        Public Sub New()
            MyBase.New
        End Sub
        
        <System.ComponentModel.Description("A path to a data controller web service."),  _
         System.ComponentModel.DefaultValue("~/Services/DataControllerService.asmx")>  _
        Public Property ServicePath() As String
            Get
                If String.IsNullOrEmpty(m_ServicePath) Then
                    Return "~/Services/DataControllerService.asmx"
                End If
                Return m_ServicePath
            End Get
            Set
                m_ServicePath = value
            End Set
        End Property
        
        Protected Overrides Sub CreateChildControls()
            MyBase.CreateChildControls()
            Dim div As HtmlGenericControl = New HtmlGenericControl("div")
            div.ID = "d"
            Controls.Add(div)
            Dim manager As MembershipManagerExtender = New MembershipManagerExtender()
            manager.ID = "b"
            manager.TargetControlID = div.ID
            manager.ServicePath = ServicePath
            Controls.Add(manager)
        End Sub
    End Class
End Namespace

Imports System
Imports System.Collections.Generic
Imports System.Collections.Specialized
Imports System.Configuration
Imports System.Configuration.Provider
Imports System.Data.Common
Imports System.Diagnostics
Imports System.Globalization
Imports System.Text.RegularExpressions
Imports System.Web
Imports System.Web.Security
Imports System.Xml.XPath
Imports TimberSmart.Data

Namespace TimberSmart.Security
    
    Partial Public Class ApplicationRoleProvider
        Inherits ApplicationRoleProviderBase
    End Class
    
    Public Enum RoleProviderSqlStatement
        
        AddUserToRole
        
        CreateRole
        
        DeleteRole
        
        DeleteRoleUsers
        
        GetAllRoles
        
        GetRolesForUser
        
        GetUsersInRole
        
        IsUserInRole
        
        RemoveUserFromRole
        
        RoleExists
        
        FindUsersInRole
    End Enum
    
    Public Class ApplicationRoleProviderBase
        Inherits RoleProvider
        
        Protected Shared Statements As SortedDictionary(Of RoleProviderSqlStatement, String) = New SortedDictionary(Of RoleProviderSqlStatement, String)()
        
        Private m_ConnectionStringSettings As ConnectionStringSettings
        
        Private m_WriteExceptionsToEventLog As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ApplicationName As String
        
        Shared Sub New()
            Statements(RoleProviderSqlStatement.AddUserToRole) = "" & ControlChars.CrLf &"insert into ie.tUserSecurityGroup(UserID, SecurityGroupID) " & ControlChars.CrLf &"values (" & ControlChars.CrLf &"     (se"& _ 
                "lect UserID from ie.tUser where UserName=@UserName) " & ControlChars.CrLf &"    ,(select SecurityGroup"& _ 
                "ID from ie.tSecurityGroup where SecurityGroupName=@RoleName)" & ControlChars.CrLf &"    " & ControlChars.CrLf &")"
            Statements(RoleProviderSqlStatement.CreateRole) = "insert into ie.tSecurityGroup (SecurityGroupName) values (@RoleName)"
            Statements(RoleProviderSqlStatement.DeleteRole) = "delete from ie.tSecurityGroup where SecurityGroupName = @RoleName"
            Statements(RoleProviderSqlStatement.DeleteRoleUsers) = "delete from ie.tUserSecurityGroup where SecurityGroupID in (select SecurityGroupI"& _ 
                "D from ie.tSecurityGroup where SecurityGroupName = @RoleName)"
            Statements(RoleProviderSqlStatement.GetAllRoles) = "select SecurityGroupName RoleName from ie.tSecurityGroup"
            Statements(RoleProviderSqlStatement.GetRolesForUser) = "" & ControlChars.CrLf &"select Roles.SecurityGroupName RoleName from ie.tSecurityGroup Roles " & ControlChars.CrLf &"inner jo"& _ 
                "in ie.tUserSecurityGroup UserRoles on Roles.SecurityGroupID = UserRoles.Security"& _ 
                "GroupID " & ControlChars.CrLf &"inner join ie.tUser Users on Users.UserID = UserRoles.UserID" & ControlChars.CrLf &"where Us"& _ 
                "ers.UserName = @UserName"
            Statements(RoleProviderSqlStatement.GetUsersInRole) = "select UserName UserName from ie.tUser where UserID in (select UserID from ie.tUs"& _ 
                "erSecurityGroup where SecurityGroupID in (select SecurityGroupID from ie.tSecuri"& _ 
                "tyGroup where SecurityGroupName = @RoleName))"
            Statements(RoleProviderSqlStatement.IsUserInRole) = "" & ControlChars.CrLf &"select count(*) from ie.tUserSecurityGroup" & ControlChars.CrLf &"where" & ControlChars.CrLf &"    UserID in (select UserID"& _ 
                " from ie.tUser where UserName = @UserName) and " & ControlChars.CrLf &"    SecurityGroupID in (select "& _ 
                "SecurityGroupID from ie.tSecurityGroup where SecurityGroupName = @RoleName)"
            Statements(RoleProviderSqlStatement.RemoveUserFromRole) = "" & ControlChars.CrLf &"delete from ie.tUserSecurityGroup" & ControlChars.CrLf &"where" & ControlChars.CrLf &"   UserID in (select UserID from ie.t"& _ 
                "User where UserName = @UserName) and" & ControlChars.CrLf &"   SecurityGroupID in (select SecurityGrou"& _ 
                "pID from ie.tSecurityGroup where SecurityGroupName = @RoleName)"
            Statements(RoleProviderSqlStatement.RoleExists) = "select count(*) from ie.tSecurityGroup where SecurityGroupName = @RoleName"
            Statements(RoleProviderSqlStatement.FindUsersInRole) = "" & ControlChars.CrLf &"select Users.UserName UserName from ie.tUser Users" & ControlChars.CrLf &"inner join ie.tUserSecurity"& _ 
                "Group UserRoles on Users.UserID= UserRoles.UserID " & ControlChars.CrLf &"inner join ie.tSecurityGroup"& _ 
                " Roles on UserRoles.SecurityGroupID = Roles.SecurityGroupID" & ControlChars.CrLf &"where " & ControlChars.CrLf &Global.Microsoft.VisualBasic.ChrW(9)&"Users.User"& _ 
                "Name like @UserName and " & ControlChars.CrLf &Global.Microsoft.VisualBasic.ChrW(9)&"Roles.SecurityGroupName = @RoleName"
        End Sub
        
        Public Overridable ReadOnly Property ConnectionStringSettings() As ConnectionStringSettings
            Get
                Return m_ConnectionStringSettings
            End Get
        End Property
        
        Public ReadOnly Property WriteExceptionsToEventLog() As Boolean
            Get
                Return m_WriteExceptionsToEventLog
            End Get
        End Property
        
        Public Overrides Property ApplicationName() As String
            Get
                Return Me.m_ApplicationName
            End Get
            Set
                Me.m_ApplicationName = value
            End Set
        End Property
        
        Protected Overridable Function CreateSqlStatement(ByVal command As RoleProviderSqlStatement) As SqlStatement
            Dim sql As SqlText = New SqlText(Statements(command), ConnectionStringSettings.Name)
            sql.Command.CommandText = sql.Command.CommandText.Replace("@", sql.ParameterMarker)
            If sql.Command.CommandText.Contains((sql.ParameterMarker + "ApplicationName")) Then
                sql.AssignParameter("ApplicationName", ApplicationName)
            End If
            sql.Name = ("TimberSmart Application Role Provider - " + command.ToString())
            sql.WriteExceptionsToEventLog = WriteExceptionsToEventLog
            Return sql
        End Function
        
        Public Overrides Sub Initialize(ByVal name As String, ByVal config As NameValueCollection)
            If (config Is Nothing) Then
                Throw New ArgumentNullException("config")
            End If
            If String.IsNullOrEmpty(name) Then
                name = "TimberSmartApplicationRoleProvider"
            End If
            If String.IsNullOrEmpty(config("description")) Then
                config.Remove("description")
                config.Add("description", "TimberSmart application role provider")
            End If
            MyBase.Initialize(name, config)
            m_ApplicationName = config("applicationName")
            If String.IsNullOrEmpty(m_ApplicationName) Then
                m_ApplicationName = System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath
            End If
            m_WriteExceptionsToEventLog = "true".Equals(config("writeExceptionsToEventLog"), StringComparison.CurrentCulture)
            m_ConnectionStringSettings = ConfigurationManager.ConnectionStrings(config("connectionStringName"))
            If ((m_ConnectionStringSettings Is Nothing) OrElse String.IsNullOrEmpty(m_ConnectionStringSettings.ConnectionString)) Then
                Throw New ProviderException("Connection string cannot be blank.")
            End If
        End Sub
        
        Public Overrides Sub AddUsersToRoles(ByVal usernames() As String, ByVal rolenames() As String)
            For Each rolename As String in rolenames
                If Not (RoleExists(rolename)) Then
                    Throw New ProviderException(String.Format("Role name '{0}' not found.", rolename))
                End If
            Next
            For Each username As String in usernames
                If username.Contains(",") Then
                    Throw New ArgumentException("User names cannot contain commas.")
                End If
                For Each rolename As String in rolenames
                    If IsUserInRole(username, rolename) Then
                        Throw New ProviderException(String.Format("User '{0}' is already in role '{1}'.", username, rolename))
                    End If
                Next
            Next
            Using sql As SqlStatement = CreateSqlStatement(RoleProviderSqlStatement.AddUserToRole)
                For Each username As String in usernames
                    ForgetUserRoles(username)
                    For Each rolename As String in rolenames
                        sql.AssignParameter("UserName", username)
                        sql.AssignParameter("RoleName", rolename)
                        sql.ExecuteNonQuery()
                    Next
                Next
            End Using
        End Sub
        
        Public Overrides Sub CreateRole(ByVal rolename As String)
            If rolename.Contains(",") Then
                Throw New ArgumentException("Role names cannot contain commas.")
            End If
            If RoleExists(rolename) Then
                Throw New ProviderException("Role already exists.")
            End If
            Using sql As SqlStatement = CreateSqlStatement(RoleProviderSqlStatement.CreateRole)
                sql.AssignParameter("RoleName", rolename)
                sql.ExecuteNonQuery()
            End Using
        End Sub
        
        Public Overrides Function DeleteRole(ByVal rolename As String, ByVal throwOnPopulatedRole As Boolean) As Boolean
            If Not (RoleExists(rolename)) Then
                Throw New ProviderException("Role does not exist.")
            End If
            If (throwOnPopulatedRole AndAlso (GetUsersInRole(rolename).Length > 0)) Then
                Throw New ProviderException("Cannot delete a pouplated role.")
            End If
            Using sql As SqlStatement = CreateSqlStatement(RoleProviderSqlStatement.DeleteRole)
                Using sql2 As SqlStatement = CreateSqlStatement(RoleProviderSqlStatement.DeleteRoleUsers)
                    sql2.AssignParameter("RoleName", rolename)
                    sql2.ExecuteNonQuery()
                End Using
                sql.AssignParameter("RoleName", rolename)
                sql.ExecuteNonQuery()
            End Using
            Return true
        End Function
        
        Public Overrides Function GetAllRoles() As String()
            Dim roles As List(Of String) = New List(Of String)()
            Using sql As SqlStatement = CreateSqlStatement(RoleProviderSqlStatement.GetAllRoles)
                Do While sql.Read()
                    roles.Add(Convert.ToString(sql("RoleName")))
                Loop
            End Using
            Return roles.ToArray()
        End Function
        
        Public Overrides Function GetRolesForUser(ByVal username As String) As String()
            Dim roles As List(Of String) = Nothing
            Dim userRolesKey As String = String.Format("ApplicationRoleProvider_{0}_Roles", username)
            Dim contextIsAvailable As Boolean = (Not (HttpContext.Current) Is Nothing)
            If contextIsAvailable Then
                roles = CType(HttpContext.Current.Items(userRolesKey),List(Of String))
            End If
            If (roles Is Nothing) Then
                roles = New List(Of String)()
                Using sql As SqlStatement = CreateSqlStatement(RoleProviderSqlStatement.GetRolesForUser)
                    sql.AssignParameter("UserName", username)
                    Do While sql.Read()
                        roles.Add(Convert.ToString(sql("RoleName")))
                    Loop
                    If contextIsAvailable Then
                        HttpContext.Current.Items(userRolesKey) = roles
                    End If
                End Using
            End If
            Return roles.ToArray()
        End Function
        
        Public Overridable Sub ForgetUserRoles(ByVal username As String)
            If (Not (HttpContext.Current) Is Nothing) Then
                HttpContext.Current.Items.Remove(String.Format("ApplicationRoleProvider_{0}_Roles", username))
            End If
        End Sub
        
        Public Overrides Function GetUsersInRole(ByVal rolename As String) As String()
            Dim users As List(Of String) = New List(Of String)()
            Using sql As SqlStatement = CreateSqlStatement(RoleProviderSqlStatement.GetUsersInRole)
                sql.AssignParameter("RoleName", rolename)
                Do While sql.Read()
                    users.Add(Convert.ToString(sql("UserName")))
                Loop
            End Using
            Return users.ToArray()
        End Function
        
        Public Overrides Function IsUserInRole(ByVal username As String, ByVal rolename As String) As Boolean
            Using sql As SqlStatement = CreateSqlStatement(RoleProviderSqlStatement.IsUserInRole)
                sql.AssignParameter("UserName", username)
                sql.AssignParameter("RoleName", rolename)
                Return (Convert.ToInt32(sql.ExecuteScalar()) > 0)
            End Using
        End Function
        
        Public Overrides Sub RemoveUsersFromRoles(ByVal usernames() As String, ByVal rolenames() As String)
            For Each rolename As String in rolenames
                If Not (RoleExists(rolename)) Then
                    Throw New ProviderException(String.Format("Role '{0}' not found.", rolename))
                End If
            Next
            For Each username As String in usernames
                For Each rolename As String in rolenames
                    If Not (IsUserInRole(username, rolename)) Then
                        Throw New ProviderException(String.Format("User '{0}' is not in role '{1}'.", username, rolename))
                    End If
                Next
            Next
            For Each username As String in usernames
                ForgetUserRoles(username)
                For Each rolename As String in rolenames
                    Using sql As SqlStatement = CreateSqlStatement(RoleProviderSqlStatement.RemoveUserFromRole)
                        sql.AssignParameter("UserName", username)
                        sql.AssignParameter("RoleName", rolename)
                        sql.ExecuteNonQuery()
                    End Using
                Next
            Next
        End Sub
        
        Public Overrides Function RoleExists(ByVal rolename As String) As Boolean
            Using sql As SqlStatement = CreateSqlStatement(RoleProviderSqlStatement.RoleExists)
                sql.AssignParameter("RoleName", rolename)
                Return (Convert.ToInt32(sql.ExecuteScalar()) > 0)
            End Using
        End Function
        
        Public Overrides Function FindUsersInRole(ByVal rolename As String, ByVal usernameToMatch As String) As String()
            Dim users As List(Of String) = New List(Of String)()
            Using sql As SqlStatement = CreateSqlStatement(RoleProviderSqlStatement.FindUsersInRole)
                sql.AssignParameter("UserName", usernameToMatch)
                sql.AssignParameter("RoleName", rolename)
                Do While sql.Read()
                    users.Add(Convert.ToString(sql("UserName")))
                Loop
            End Using
            Return users.ToArray()
        End Function
    End Class
End Namespace

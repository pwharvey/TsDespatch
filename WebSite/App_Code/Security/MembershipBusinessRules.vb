Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Net.Mail
Imports System.Text
Imports System.Web
Imports System.Web.Security
Imports TimberSmart.Data
Imports TimberSmart.Services

Namespace TimberSmart.Security
    
    Partial Public Class MembershipBusinessRules
        Inherits MembershipBusinessRulesBase
    End Class
    
    Public Class MembershipBusinessRulesBase
        Inherits BusinessRules
        
        Public Shared CreateErrors As SortedDictionary(Of MembershipCreateStatus, String) = New SortedDictionary(Of MembershipCreateStatus, String)()
        
        Shared Sub New()
            CreateErrors.Add(MembershipCreateStatus.DuplicateEmail, "Duplicate email address.")
            CreateErrors.Add(MembershipCreateStatus.DuplicateProviderUserKey, "Duplicate provider key.")
            CreateErrors.Add(MembershipCreateStatus.DuplicateUserName, "Duplicate user name.")
            CreateErrors.Add(MembershipCreateStatus.InvalidAnswer, "Invalid password recovery answer.")
            CreateErrors.Add(MembershipCreateStatus.InvalidEmail, "Invalid email address.")
            CreateErrors.Add(MembershipCreateStatus.InvalidPassword, "Invalid password.")
            CreateErrors.Add(MembershipCreateStatus.InvalidProviderUserKey, "Invalid provider user key.")
            CreateErrors.Add(MembershipCreateStatus.InvalidQuestion, "Invalid password recovery question.")
            CreateErrors.Add(MembershipCreateStatus.InvalidUserName, "Invalid user name.")
            CreateErrors.Add(MembershipCreateStatus.ProviderError, "Provider error.")
            CreateErrors.Add(MembershipCreateStatus.UserRejected, "User has been rejected.")
        End Sub
        
        <RowFilter("aspnet_Membership", "signUpForm", "UserName"),  _
         RowFilter("aspnet_Membership", "passwordRequestForm", "UserName")>  _
        Protected Overridable ReadOnly Property FakeUserName() As String
            Get
                'We will filter user rows by a fake name to prevent 
                'return of any existing user records through sign-up
                'or password recovery forms
                Return "__No_Name"
            End Get
        End Property
        
        <RowFilter("aspnet_Membership", "identityConfirmationForm", "UserName")>  _
        Protected Overridable ReadOnly Property IdentityConfirmationUserName() As String
            Get
                'We are filtering rows by a user name provided to PasswordRequest method
                Return CType(Context.Session("IdentityConfirmation"),String)
            End Get
        End Property
        
        <RowFilter("aspnet_Membership", "myAccountForm", "UserName")>  _
        Protected Overridable ReadOnly Property CurrentUserName() As String
            Get
                Return Context.User.Identity.Name
            End Get
        End Property
        
        <ControllerAction("aspnet_Membership", "Delete", ActionPhase.Before)>  _
        Protected Overridable Sub DeleteUser(ByVal userId As Guid)
            PreventDefault()
            Dim user As MembershipUser = Membership.GetUser(userId)
            Membership.DeleteUser(user.UserName)
            Result.ShowLastView()
            Result.ShowMessage(String.Format(Localize("UserHasBeenDeleted", "User '{0}' has been deleted."), user.UserName))
        End Sub
        
        <ControllerAction("aspnet_Membership", "Update", ActionPhase.Before)>  _
        Protected Overridable Sub UpdateUser(ByVal userId As Guid, ByVal email As FieldValue, ByVal isApproved As FieldValue, ByVal isLockedOut As FieldValue, ByVal comment As FieldValue, ByVal roles As FieldValue)
            PreventDefault()
            Dim user As MembershipUser = Membership.GetUser(userId)
            'update user information
            If email.Modified Then
                user.Email = Convert.ToString(email.Value)
                Membership.UpdateUser(user)
            End If
            If isApproved.Modified Then
                user.IsApproved = Convert.ToBoolean(isApproved.Value)
                Membership.UpdateUser(user)
            End If
            If isLockedOut.Modified Then
                If Convert.ToBoolean(isLockedOut.Value) Then
                    Result.Focus("IsLockedOut", Localize("UserCannotBeLockedOut", "User cannot be locked out. If you want to prevent this user from being able to lo"& _ 
                                "gin then simply mark user as 'not-approved'."))
                    Throw New Exception(Localize("ErrorSavingUser", "Error saving user account."))
                End If
                user.UnlockUser()
            End If
            If comment.Modified Then
                user.Comment = Convert.ToString(comment.Value)
                Membership.UpdateUser(user)
            End If
            If roles.Modified Then
                Dim newRoles() As String = Convert.ToString(roles.Value).Split(Global.Microsoft.VisualBasic.ChrW(44))
                Dim oldRoles() As String = System.Web.Security.Roles.GetRolesForUser(user.UserName)
                For Each role As String in oldRoles
                    If (Not (String.IsNullOrEmpty(role)) AndAlso (Array.IndexOf(newRoles, role) = -1)) Then
                        System.Web.Security.Roles.RemoveUserFromRole(user.UserName, role)
                    End If
                Next
                For Each role As String in newRoles
                    If (Not (String.IsNullOrEmpty(role)) AndAlso (Array.IndexOf(oldRoles, role) = -1)) Then
                        System.Web.Security.Roles.AddUserToRole(user.UserName, role)
                    End If
                Next
            End If
        End Sub
        
        <ControllerAction("aspnet_Membership", "Insert", ActionPhase.Before)>  _
        Protected Overridable Sub InsertUser(ByVal username As String, ByVal password As String, ByVal confirmPassword As String, ByVal email As String, ByVal passwordQuestion As String, ByVal passwordAnswer As String, ByVal isApproved As Boolean, ByVal comment As String, ByVal roles As String)
            PreventDefault()
            If Not ((password = confirmPassword)) Then
                Throw New Exception(Localize("PasswordAndConfirmationDoNotMatch", "Password and confirmation do not match."))
            End If
            'create a user
            Dim status As MembershipCreateStatus
            Membership.CreateUser(username, password, email, passwordQuestion, passwordAnswer, isApproved, status)
            If Not ((status = MembershipCreateStatus.Success)) Then
                Throw New Exception(Localize(status.ToString(), MembershipBusinessRules.CreateErrors(status)))
            End If
            'retrieve the primary key of the new user account
            Dim newUser As MembershipUser = Membership.GetUser(username)
            Dim providerUserKey As Object = newUser.ProviderUserKey
            If TypeOf providerUserKey Is Byte Then
                providerUserKey = New Guid(CType(providerUserKey,Byte()))
            End If
            Result.Values.Add(New FieldValue("UserId", providerUserKey))
            'update a comment
            If Not (String.IsNullOrEmpty(comment)) Then
                newUser.Comment = comment
                Membership.UpdateUser(newUser)
            End If
            If Not (String.IsNullOrEmpty(roles)) Then
                For Each role As String in roles.Split(Global.Microsoft.VisualBasic.ChrW(44))
                    System.Web.Security.Roles.AddUserToRole(username, role)
                Next
            End If
        End Sub
        
        <ControllerAction("aspnet_Membership", "signUpForm", "Insert", ActionPhase.Before)>  _
        Protected Overridable Sub SignUpUser(ByVal username As String, ByVal password As String, ByVal confirmPassword As String, ByVal email As String, ByVal passwordQuestion As String, ByVal passwordAnswer As String)
            InsertUser(username, password, confirmPassword, email, passwordQuestion, passwordAnswer, true, Localize("SelfRegisteredUser", "Self-registered user."), "Users")
        End Sub
        
        <RowBuilder("aspnet_Membership", "createForm1", RowKind.New)>  _
        Protected Overridable Sub NewUserRow()
            UpdateFieldValue("UserName", "user name")
            UpdateFieldValue("IsApproved", true)
        End Sub
        
        <RowBuilder("aspnet_Membership", "editForm1", RowKind.Existing)>  _
        Protected Overridable Sub PrepareUserRow()
            Dim userName As String = CType(SelectFieldValue("UserUserName"),String)
            Dim sb As StringBuilder = New StringBuilder()
            For Each role As String in System.Web.Security.Roles.GetRolesForUser(userName)
                If (sb.Length > 0) Then
                    sb.Append(Global.Microsoft.VisualBasic.ChrW(44))
                End If
                sb.Append(role)
            Next
            UpdateFieldValue("Roles", sb.ToString())
            Dim dt As DateTime = CType(SelectFieldValue("LastLockoutDate"),DateTime)
            If dt.Equals(New DateTime(1754, 1, 1)) Then
                UpdateFieldValue("LastLockoutDate", Nothing)
            End If
            dt = CType(SelectFieldValue("FailedPasswordAttemptWindowStart"),DateTime)
            If dt.Equals(New DateTime(1754, 1, 1)) Then
                UpdateFieldValue("FailedPasswordAttemptWindowStart", Nothing)
            End If
            dt = CType(SelectFieldValue("FailedPasswordAnswerAttemptWindowStart"),DateTime)
            If dt.Equals(New DateTime(1754, 1, 1)) Then
                UpdateFieldValue("FailedPasswordAnswerAttemptWindowStart", Nothing)
            End If
        End Sub
        
        <ControllerAction("aspnet_Roles", "Insert", ActionPhase.Before)>  _
        Protected Overridable Sub InsertRole(ByVal roleName As String)
            PreventDefault()
            System.Web.Security.Roles.CreateRole(roleName)
        End Sub
        
        <ControllerAction("aspnet_Roles", "Delete", ActionPhase.Before)>  _
        Protected Overridable Sub DeleteRole(ByVal roleName As String)
            PreventDefault()
            System.Web.Security.Roles.DeleteRole(roleName)
        End Sub
        
        <RowBuilder("aspnet_Membership", "passwordRequestForm", RowKind.New)>  _
        Protected Overridable Sub NewPasswordRequestRow()
            UpdateFieldValue("UserName", Context.Session("IdentityConfirmation"))
        End Sub
        
        <ControllerAction("aspnet_Membership", "passwordRequestForm", "Custom", "RequestPassword", ActionPhase.Execute)>  _
        Protected Overridable Sub PasswordRequest(ByVal userName As String)
            PreventDefault()
            Dim user As MembershipUser = Membership.GetUser(userName)
            If (user Is Nothing) Then
                Result.ShowAlert(Localize("UserNameDoesNotExist", "User name does not exist."), "UserName")
            Else
                Context.Session("IdentityConfirmation") = userName
                Result.HideModal()
                Result.ShowModal("aspnet_Membership", "identityConfirmationForm", "Edit", "identityConfirmationForm")
            End If
        End Sub
        
        <RowBuilder("aspnet_Membership", "identityConfirmationForm", RowKind.Existing)>  _
        Protected Overridable Sub PrepareIdentityConfirmationRow()
            UpdateFieldValue("PasswordAnswer", Nothing)
        End Sub
        
        <ControllerAction("aspnet_Membership", "identityConfirmationForm", "Custom", "ConfirmIdentity", ActionPhase.Execute)>  _
        Protected Overridable Sub IdentityConfirmation(ByVal userName As String, ByVal passwordAnswer As String)
            PreventDefault()
            Dim user As MembershipUser = Membership.GetUser(userName)
            If (Not (user) Is Nothing) Then
                Dim newPassword As String = user.ResetPassword(passwordAnswer)
                'create an email and send it to the user
                Dim message As MailMessage = New MailMessage()
                message.To.Add(user.Email)
                message.Subject = String.Format(Localize("NewPasswordSubject", "New password for '{0}'."), userName)
                message.Body = newPassword
                Dim client As SmtpClient = New SmtpClient()
                client.Send(message)
                'hide modal popup and display a confirmation
                Result.HideModal()
                Result.ShowAlert(Localize("NewPasswordAlert", "A new password has been emailed to the address on file."))
            End If
        End Sub
        
        <RowBuilder("aspnet_Membership", "myAccountForm", RowKind.Existing)>  _
        Protected Overridable Sub PrepareCurrentUserRow()
            UpdateFieldValue("OldPassword", Nothing)
            UpdateFieldValue("Password", Nothing)
            UpdateFieldValue("ConfirmPassword", Nothing)
            UpdateFieldValue("PasswordAnswer", Nothing)
        End Sub
        
        <ControllerAction("aspnet_Membership", "identityConfirmationForm", "Custom", "BackToRequestPassword", ActionPhase.Execute)>  _
        Protected Overridable Sub BackToRequestPassword()
            PreventDefault()
            Result.HideModal()
            Result.ShowModal("aspnet_Membership", "passwordRequestForm", "New", "passwordRequestForm")
        End Sub
        
        <ControllerAction("aspnet_Membership", "myAccountForm", "Update", ActionPhase.Before)>  _
        Protected Overridable Sub UpdateMyAccount(ByVal userName As String, ByVal oldPassword As String, ByVal password As String, ByVal confirmPassword As String, ByVal email As String, ByVal passwordQuestion As String, ByVal passwordAnswer As String)
            PreventDefault()
            Dim user As MembershipUser = Membership.GetUser(userName)
            If (Not (user) Is Nothing) Then
                If String.IsNullOrEmpty(oldPassword) Then
                    Result.ShowAlert(Localize("EnterCurrentPassword", "Please enter your current password."), "OldPassword")
                    Return
                End If
                If Not (Membership.ValidateUser(userName, oldPassword)) Then
                    Result.ShowAlert(Localize("PasswordDoesNotMatchRecords", "Your password does not match our records."), "OldPassword")
                    Return
                End If
                If (Not (String.IsNullOrEmpty(password)) OrElse Not (String.IsNullOrEmpty(confirmPassword))) Then
                    If Not ((password = confirmPassword)) Then
                        Result.ShowAlert(Localize("NewPasswordAndConfirmatinDoNotMatch", "New password and confirmation do not match."), "Password")
                        Return
                    End If
                    If Not (user.ChangePassword(oldPassword, password)) Then
                        Result.ShowAlert(Localize("NewPasswordInvalid", "Your new password is invalid."), "Password")
                        Return
                    End If
                End If
                If Not ((email = user.Email)) Then
                    user.Email = email
                    Membership.UpdateUser(user)
                End If
                If (Not ((user.PasswordQuestion = passwordQuestion)) AndAlso String.IsNullOrEmpty(passwordAnswer)) Then
                    Result.ShowAlert(Localize("EnterPasswordAnswer", "Please enter a password answer."), "PasswordAnswer")
                    Return
                End If
                If Not (String.IsNullOrEmpty(passwordAnswer)) Then
                    user.ChangePasswordQuestionAndAnswer(oldPassword, passwordQuestion, passwordAnswer)
                    Membership.UpdateUser(user)
                End If
                Result.HideModal()
            Else
                Result.ShowAlert(Localize("UserNotFound", "User not found."))
            End If
        End Sub
        
        Public Shared Sub CreateStandardMembershipAccounts()
            ApplicationServices.RegisterStandardMembershipAccounts()
        End Sub
        
        <ControllerAction("aspnet_Membership", "Select", ActionPhase.Before)>  _
        Public Overridable Sub AccessControlValidation()
            If Context.User.Identity.IsAuthenticated Then
                Return
            End If
            If Not ((((Request.View = "signUpForm") OrElse (Request.View = "passwordRequestForm")) OrElse ((Request.View = "identityConfirmationForm") OrElse (Request.View = "loginForm")))) Then
                Throw New Exception("Not authorized")
            End If
        End Sub
    End Class
End Namespace

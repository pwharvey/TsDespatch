﻿Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Web
Imports System.Web.UI
Imports System.Xml.XPath
Imports TimberSmart.Data
Imports TimberSmart.Services

Namespace TimberSmart.Handlers
    
    Partial Public Class BlobFactory
        
        Shared Sub New()
            'register blob handlers
        End Sub
    End Class
    
    Public Enum BlobMode
        
        Thumbnail
        
        Original
        
        Upload
    End Enum
    
    Public Class BlobHandlerInfo
        
        Private m_Key As String
        
        Private m_TableName As String
        
        Private m_FieldName As String
        
        Private m_KeyFieldNames() As String
        
        Private m_Text As String
        
        Private m_ContentType As String
        
        Private m_DataController As String
        
        Private m_ControllerFieldName As String
        
        Public Sub New()
            MyBase.New
        End Sub
        
        Public Sub New(ByVal key As String, ByVal tableName As String, ByVal fieldName As String, ByVal keyFieldNames() As String, ByVal text As String, ByVal contentType As String)
            Me.New(key, tableName, fieldName, keyFieldNames, text, contentType, String.Empty, String.Empty)
        End Sub
        
        Public Sub New(ByVal key As String, ByVal tableName As String, ByVal fieldName As String, ByVal keyFieldNames() As String, ByVal text As String, ByVal contentType As String, ByVal dataController As String, ByVal controllerFieldName As String)
            MyBase.New
            Me.Key = key
            Me.TableName = tableName
            Me.FieldName = fieldName
            Me.KeyFieldNames = keyFieldNames
            Me.Text = text
            Me.m_ContentType = contentType
            Me.DataController = dataController
            Me.ControllerFieldName = controllerFieldName
        End Sub
        
        Public Overridable Property Key() As String
            Get
                Return m_Key
            End Get
            Set
                m_Key = value
            End Set
        End Property
        
        Protected Default Property Item(ByVal name As String) As String
            Get
                Return CType(HttpContext.Current.Items(("BlobHandlerInfo_" + name)),String)
            End Get
            Set
                HttpContext.Current.Items(("BlobHandlerInfo_" + name)) = value
            End Set
        End Property
        
        Public Overridable Property TableName() As String
            Get
                Return m_TableName
            End Get
            Set
                m_TableName = value
            End Set
        End Property
        
        Public Overridable Property FieldName() As String
            Get
                Return m_FieldName
            End Get
            Set
                m_FieldName = value
            End Set
        End Property
        
        Public Overridable Property KeyFieldNames() As String()
            Get
                Return m_KeyFieldNames
            End Get
            Set
                m_KeyFieldNames = value
            End Set
        End Property
        
        Public Overridable Property Text() As String
            Get
                Return m_Text
            End Get
            Set
                m_Text = value
            End Set
        End Property
        
        Public Overridable Property [Error]() As String
            Get
                Return Me("Error")
            End Get
            Set
                Me("Error") = value
            End Set
        End Property
        
        Public Overridable Property FileName() As String
            Get
                Return Me("FileName")
            End Get
            Set
                Me("FileName") = value
            End Set
        End Property
        
        Public Overridable Property ContentType() As String
            Get
                Dim s As String = Me("ContentType")
                If String.IsNullOrEmpty(s) Then
                    s = Me.m_ContentType
                End If
                Return s
            End Get
            Set
                Me("ContentType") = value
            End Set
        End Property
        
        Public Overridable Property DataController() As String
            Get
                Return m_DataController
            End Get
            Set
                m_DataController = value
            End Set
        End Property
        
        Public Overridable ReadOnly Property UploadDownloadViewName() As String
            Get
                Return Controller.GetUpdateView(DataController)
            End Get
        End Property
        
        Public Overridable Property ControllerFieldName() As String
            Get
                Return m_ControllerFieldName
            End Get
            Set
                m_ControllerFieldName = value
            End Set
        End Property
        
        Public Shared ReadOnly Property Current() As BlobHandlerInfo
            Get
                Dim d As BlobHandlerInfo = CType(HttpContext.Current.Items("BlobHandlerInfo_Current"),BlobHandlerInfo)
                If (d Is Nothing) Then
                    For Each key As String in HttpContext.Current.Request.QueryString.Keys
                        If (Not (String.IsNullOrEmpty(key)) AndAlso BlobFactory.Handlers.ContainsKey(key)) Then
                            d = BlobFactory.Handlers(key)
                            HttpContext.Current.Items("BlobHandlerInfo_Current") = d
                            Exit For
                        End If
                    Next
                End If
                Return d
            End Get
        End Property
        
        Public ReadOnly Property Mode() As BlobMode
            Get
                If Value.StartsWith("u|") Then
                    Return BlobMode.Upload
                End If
                If Value.StartsWith("t|") Then
                    Return BlobMode.Thumbnail
                Else
                    Return BlobMode.Original
                End If
            End Get
        End Property
        
        Public ReadOnly Property Value() As String
            Get
                Dim v As String = Me("Value")
                If String.IsNullOrEmpty(v) Then
                    v = HttpContext.Current.Request.QueryString(Key)
                End If
                Return v
            End Get
        End Property
        
        Public ReadOnly Property Reference() As String
            Get
                Dim s As String = Value.Replace("|", "_")
                Return s.Substring(1)
            End Get
        End Property
        
        Public Overridable Property ContentTypeField() As String
            Get
                Dim fieldName As String = Me((ControllerFieldName + "_ContentTypeField"))
                If Not (String.IsNullOrEmpty(fieldName)) Then
                    Return fieldName
                End If
                Return (ControllerFieldName + "ContentType")
            End Get
            Set
                Me((ControllerFieldName + "_ContentTypeField")) = value
            End Set
        End Property
        
        Public Overridable Property FileNameField() As String
            Get
                Dim fieldName As String = Me((ControllerFieldName + "_FileNameField"))
                If Not (String.IsNullOrEmpty(fieldName)) Then
                    Return fieldName
                End If
                Return (m_ControllerFieldName + "FileName")
            End Get
            Set
                Me((ControllerFieldName + "_FileNameField")) = value
            End Set
        End Property
        
        Public Overridable Property LengthField() As String
            Get
                Dim fieldName As String = Me((ControllerFieldName + "_LengthField"))
                If Not (String.IsNullOrEmpty(fieldName)) Then
                    Return fieldName
                End If
                Return (m_ControllerFieldName + "Length")
            End Get
            Set
                Me((ControllerFieldName + "_LengthField")) = value
            End Set
        End Property
        
        Public Overloads Overridable Function SaveFile(ByVal context As HttpContext) As Boolean
            Return Me.SaveFile(context, Nothing, Nothing)
        End Function
        
        Public Overloads Overridable Function SaveFile(ByVal context As HttpContext, ByVal ba As BlobAdapter, ByVal keyValue As String) As Boolean
            If Not ((context.Request.Files.Count = 1)) Then
                Return false
            End If
            Try 
                If ((Not (BlobHandlerInfo.Current) Is Nothing) AndAlso BlobHandlerInfo.Current.ProcessUploadViaBusinessRule(ba)) Then
                    Return true
                End If
                If (ba Is Nothing) Then
                    Using updateBlob As SqlStatement = BlobFactory.CreateBlobUpdateStatement()
                        Return (updateBlob.ExecuteNonQuery() = 1)
                    End Using
                Else
                    Dim file As HttpPostedFile = context.Request.Files(0)
                    If file.ContentLength.Equals(0) Then
                        Return true
                    End If
                    Return ba.WriteBlob(file, keyValue)
                End If
            Catch err As Exception
                [Error] = err.Message
                Return false
            End Try
        End Function
        
        Public Function CreateKeyValues() As List(Of String)
            Dim keyValues As List(Of String) = New List(Of String)()
            Dim m As Match = Regex.Match(Value, "\|([\w-]+)")
            Do While m.Success
                keyValues.Add(m.Groups(1).Value)
                m = m.NextMatch()
            Loop
            Return keyValues
        End Function
        
        Private Function CreateActionValues(ByVal stream As Stream, ByVal contentType As String, ByVal fileName As String, ByVal contentLength As Integer) As List(Of FieldValue)
            Dim deleting As Boolean = (((contentType = "application/octet-stream") AndAlso (contentLength = 0)) AndAlso String.IsNullOrEmpty(fileName))
            Dim keyValues As List(Of String) = CreateKeyValues()
            Dim keyValueIndex As Integer = 0
            Dim actionValues As List(Of FieldValue) = New List(Of FieldValue)()
            Dim config As ControllerConfiguration = Controller.CreateConfigurationInstance(GetType(Controller), DataController)
            Dim keyFieldIterator As XPathNodeIterator = config.Select("/c:dataController/c:fields/c:field[@isPrimaryKey='true']")
            Do While keyFieldIterator.MoveNext()
                Dim v As FieldValue = New FieldValue(keyFieldIterator.Current.GetAttribute("name", String.Empty))
                If (keyValueIndex < keyValues.Count) Then
                    v.OldValue = keyValues(keyValueIndex)
                    v.Modified = false
                    keyValueIndex = (keyValueIndex + 1)
                End If
                actionValues.Add(v)
            Loop
            If (Not (stream) Is Nothing) Then
                Dim lengthFieldNav As XPathNavigator = config.SelectSingleNode("/c:dataController/c:fields/c:field[@name='{0}']", Me.LengthField)
                If (lengthFieldNav Is Nothing) Then
                    lengthFieldNav = config.SelectSingleNode("/c:dataController/c:fields/c:field[@name='{0}Length' or @name='{0}LENGTH']", ControllerFieldName)
                End If
                If (lengthFieldNav Is Nothing) Then
                    lengthFieldNav = config.SelectSingleNode("/c:dataController/c:fields/c:field[@name='Length' or @name='LENGTH']", ControllerFieldName)
                End If
                If (Not (lengthFieldNav) Is Nothing) Then
                    Dim fieldName As String = lengthFieldNav.GetAttribute("name", String.Empty)
                    If Not ((fieldName = Me.LengthField)) Then
                        Me.LengthField = fieldName
                    End If
                    actionValues.Add(New FieldValue(fieldName, contentLength))
                    If deleting Then
                        ClearLastFieldValue(actionValues)
                    End If
                End If
                Dim contentTypeFieldNav As XPathNavigator = config.SelectSingleNode("/c:dataController/c:fields/c:field[@name='{0}']", Me.ContentTypeField)
                If (contentTypeFieldNav Is Nothing) Then
                    contentTypeFieldNav = config.SelectSingleNode("/c:dataController/c:fields/c:field[@name='{0}ContentType' or @name='{0}CONTENTTYP"& _ 
                            "E']", ControllerFieldName)
                End If
                If (contentTypeFieldNav Is Nothing) Then
                    contentTypeFieldNav = config.SelectSingleNode("/c:dataController/c:fields/c:field[@name='ContentType' or @name='CONTENTTYPE']", ControllerFieldName)
                End If
                If (Not (contentTypeFieldNav) Is Nothing) Then
                    Dim fieldName As String = contentTypeFieldNav.GetAttribute("name", String.Empty)
                    If Not ((fieldName = Me.ContentTypeField)) Then
                        Me.ContentTypeField = fieldName
                    End If
                    actionValues.Add(New FieldValue(fieldName, contentType))
                    If deleting Then
                        ClearLastFieldValue(actionValues)
                    End If
                End If
                Dim fileNameFieldNav As XPathNavigator = config.SelectSingleNode("/c:dataController/c:fields/c:field[@name='{0}']", Me.FileNameField)
                If (fileNameFieldNav Is Nothing) Then
                    fileNameFieldNav = config.SelectSingleNode("/c:dataController/c:fields/c:field[@name='{0}FileName' or @name='{0}FILENAME']", ControllerFieldName)
                End If
                If (fileNameFieldNav Is Nothing) Then
                    fileNameFieldNav = config.SelectSingleNode("/c:dataController/c:fields/c:field[@name='FileName' or @name='FILENAME']", ControllerFieldName)
                End If
                If (Not (fileNameFieldNav) Is Nothing) Then
                    Dim fieldName As String = fileNameFieldNav.GetAttribute("name", String.Empty)
                    If Not ((fieldName = Me.FileNameField)) Then
                        Me.FileNameField = fieldName
                    End If
                    actionValues.Add(New FieldValue(fieldName, Path.GetFileName(fileName)))
                    If deleting Then
                        ClearLastFieldValue(actionValues)
                    End If
                End If
                actionValues.Add(New FieldValue(ControllerFieldName, stream))
            End If
            Return actionValues
        End Function
        
        Private Sub ClearLastFieldValue(ByVal values As List(Of FieldValue))
            Dim v As FieldValue = values((values.Count - 1))
            v.NewValue = Nothing
            v.Modified = true
        End Sub
        
        Private Function ProcessUploadViaBusinessRule(ByVal ba As BlobAdapter) As Boolean
            Dim file As HttpPostedFile = HttpContext.Current.Request.Files(0)
            Dim actionValues As List(Of FieldValue) = CreateActionValues(file.InputStream, file.ContentType, file.FileName, file.ContentLength)
            If (Not (ba) Is Nothing) Then
                For Each fv As FieldValue in actionValues
                    ba.ValidateFieldValue(fv)
                Next
            End If
            'try process uploading via a business rule
            Dim args As ActionArgs = New ActionArgs()
            args.Controller = DataController
            args.CommandName = "UploadFile"
            args.CommandArgument = ControllerFieldName
            args.Values = actionValues.ToArray()
            Dim r As ActionResult = Blob.CreateDataController().Execute(DataController, UploadDownloadViewName, args)
            Dim supportsContentType As Boolean = false
            Dim supportsFileName As Boolean = false
            DetectSupportForSpecialFields(actionValues, supportsContentType, supportsFileName)
            Dim canceled As Boolean = r.Canceled
            If (canceled AndAlso Not ((supportsContentType OrElse supportsFileName))) Then
                Return true
            End If
            'update Content Type and Length
            args.LastCommandName = "Edit"
            args.CommandName = "Update"
            args.CommandArgument = UploadDownloadViewName
            actionValues.RemoveAt((actionValues.Count - 1))
            If HttpContext.Current.Request.Url.ToString().EndsWith("&_v=2") Then
                For Each v As FieldValue in actionValues
                    If (v.Name = FileNameField) Then
                        actionValues.Remove(v)
                        Exit For
                    End If
                Next
            End If
            args.Values = actionValues.ToArray()
            args.IgnoreBusinessRules = true
            r = Blob.CreateDataController().Execute(DataController, UploadDownloadViewName, args)
            Return canceled
        End Function
        
        Public Overridable Sub LoadFile(ByVal stream As Stream)
            If ((Not (BlobHandlerInfo.Current) Is Nothing) AndAlso BlobHandlerInfo.Current.ProcessDownloadViaBusinessRule(stream)) Then
                Return
            End If
            Using getBlob As SqlStatement = BlobFactory.CreateBlobSelectStatement()
                If getBlob.Read() Then
                    Dim v As Object = getBlob(0)
                    If Not (DBNull.Value.Equals(v)) Then
                        If GetType(String).Equals(getBlob.Reader.GetFieldType(0)) Then
                            Dim stringData() As Byte = Encoding.Default.GetBytes(CType(v,String))
                            stream.Write(stringData, 0, stringData.Length)
                        Else
                            Dim data() As Byte = CType(v,Byte())
                            stream.Write(data, 0, data.Length)
                        End If
                    End If
                End If
            End Using
        End Sub
        
        Private Sub DetectSupportForSpecialFields(ByVal values As List(Of FieldValue), ByRef supportsContentType As Boolean, ByRef supportsFileName As Boolean)
            supportsContentType = false
            supportsFileName = false
            For Each v As FieldValue in values
                If v.Name.Equals(ContentTypeField, StringComparison.OrdinalIgnoreCase) Then
                    supportsContentType = true
                Else
                    If v.Name.Equals(FileNameField, StringComparison.OrdinalIgnoreCase) Then
                        supportsFileName = true
                    End If
                End If
            Next
        End Sub
        
        Public Function ProcessDownloadViaBusinessRule(ByVal stream As Stream) As Boolean
            Dim supportsContentType As Boolean = false
            Dim supportsFileName As Boolean = false
            Dim actionValues As List(Of FieldValue) = CreateActionValues(stream, Nothing, Nothing, 0)
            DetectSupportForSpecialFields(actionValues, supportsContentType, supportsFileName)
            'try processing download via a business rule
            Dim args As ActionArgs = New ActionArgs()
            args.Controller = DataController
            args.CommandName = "DownloadFile"
            args.CommandArgument = ControllerFieldName
            args.Values = actionValues.ToArray()
            Dim r As ActionResult = Blob.CreateDataController().Execute(DataController, UploadDownloadViewName, args)
            For Each v As FieldValue in r.Values
                If v.Name.Equals(ContentTypeField, StringComparison.OrdinalIgnoreCase) Then
                    Current.ContentType = Convert.ToString(v.Value)
                Else
                    If v.Name.Equals(FileNameField, StringComparison.OrdinalIgnoreCase) Then
                        Current.FileName = Convert.ToString(v.Value)
                    End If
                End If
            Next
            'see if we still need to retrieve the content type or the file name from the database
            Dim needsContentType As Boolean = String.IsNullOrEmpty(Current.ContentType)
            Dim needsFileName As Boolean = String.IsNullOrEmpty(Current.FileName)
            If ((needsContentType AndAlso supportsContentType) OrElse (needsFileName AndAlso supportsFileName)) Then
                actionValues = CreateActionValues(Nothing, Nothing, Nothing, 0)
                Dim filter As List(Of String) = New List(Of String)()
                For Each v As FieldValue in actionValues
                    filter.Add(String.Format("{0}:={1}", v.Name, v.Value))
                Next
                Dim request As PageRequest = New PageRequest()
                request.Controller = DataController
                request.View = UploadDownloadViewName
                request.PageSize = 1
                request.RequiresMetaData = true
                request.Filter = filter.ToArray()
                Dim page As ViewPage = Blob.CreateDataController().GetPage(request.Controller, request.View, request)
                If (page.Rows.Count = 1) Then
                    Dim row() As Object = page.Rows(0)
                    If supportsContentType Then
                        Current.ContentType = Convert.ToString(page.SelectFieldValue(ContentTypeField, row))
                    End If
                    If supportsFileName Then
                        Current.FileName = Convert.ToString(page.SelectFieldValue(FileNameField, row))
                    End If
                End If
            End If
            Return r.Canceled
        End Function
    End Class
    
    Partial Public Class BlobFactory
        
        Public Shared Handlers As SortedDictionary(Of String, BlobHandlerInfo) = New SortedDictionary(Of String, BlobHandlerInfo)()
        
        Public Overloads Shared Sub RegisterHandler(ByVal key As String, ByVal tableName As String, ByVal fieldName As String, ByVal keyFieldNames() As String, ByVal text As String, ByVal contentType As String)
            Handlers.Add(key, New BlobHandlerInfo(key, tableName, fieldName, keyFieldNames, text, contentType))
        End Sub
        
        Public Overloads Shared Sub RegisterHandler(ByVal key As String, ByVal tableName As String, ByVal fieldName As String, ByVal keyFieldNames() As String, ByVal text As String, ByVal dataController As String, ByVal controllerFieldName As String)
            Handlers.Add(key, New BlobHandlerInfo(key, tableName, fieldName, keyFieldNames, text, String.Empty, dataController, controllerFieldName))
        End Sub
        
        Public Shared Function CreateBlobSelectStatement() As SqlStatement
            Dim handler As BlobHandlerInfo = BlobHandlerInfo.Current
            If (Not (handler) Is Nothing) Then
                Dim parameterMarker As String = SqlStatement.GetParameterMarker(String.Empty)
                Dim keyValues As List(Of String) = handler.CreateKeyValues()
                Dim sb As StringBuilder = New StringBuilder()
                sb.AppendFormat("select {0} from {1} where ", handler.FieldName, handler.TableName)
                Dim i As Integer = 0
                Do While (i < handler.KeyFieldNames.Length)
                    If (i > 0) Then
                        sb.Append(" and ")
                    End If
                    sb.AppendFormat("{0}={1}p{2}", handler.KeyFieldNames(i), parameterMarker, i)
                    i = (i + 1)
                Loop
                Dim getBlob As SqlText = New SqlText(sb.ToString())
                Dim j As Integer = 0
                Do While (j < handler.KeyFieldNames.Length)
                    getBlob.AddParameter(String.Format("{0}p{1}", parameterMarker, j), getBlob.StringToValue(keyValues(j)))
                    j = (j + 1)
                Loop
                Return getBlob
            End If
            Return Nothing
        End Function
        
        Public Shared Function CreateBlobUpdateStatement() As SqlStatement
            Dim handler As BlobHandlerInfo = BlobHandlerInfo.Current
            If (Not (handler) Is Nothing) Then
                Dim parameterMarker As String = SqlStatement.GetParameterMarker(String.Empty)
                Dim keyValues As List(Of String) = handler.CreateKeyValues()
                Dim file As HttpPostedFile = HttpContext.Current.Request.Files(0)
                Dim sb As StringBuilder = New StringBuilder()
                sb.AppendFormat("update {0} set {1} = ", handler.TableName, handler.FieldName)
                If (file.ContentLength = 0) Then
                    sb.Append("null")
                Else
                    sb.AppendFormat("{0}blob", parameterMarker)
                End If
                sb.Append(" where ")
                Dim i As Integer = 0
                Do While (i < handler.KeyFieldNames.Length)
                    If (i > 0) Then
                        sb.Append(" and ")
                    End If
                    sb.AppendFormat("{0}={1}p{2}", handler.KeyFieldNames(i), parameterMarker, i)
                    i = (i + 1)
                Loop
                Dim updateBlob As SqlText = New SqlText(sb.ToString())
                If (file.ContentLength > 0) Then
                    Dim data((file.ContentLength) - 1) As Byte
                    file.InputStream.Read(data, 0, data.Length)
                    updateBlob.AddParameter((parameterMarker + "blob"), data)
                End If
                Dim j As Integer = 0
                Do While (j < handler.KeyFieldNames.Length)
                    updateBlob.AddParameter(String.Format("{0}p{1}", parameterMarker, j), updateBlob.StringToValue(keyValues(j)))
                    j = (j + 1)
                Loop
                Return updateBlob
            End If
            Return Nothing
        End Function
    End Class
    
    Public Class Blob
        Inherits GenericHandlerBase
        Implements IHttpHandler, System.Web.SessionState.IRequiresSessionState
        
        Public Const ThumbnailCacheTimeout As Integer = 5
        
        Public Shared ImageFormats As SortedDictionary(Of Guid, String)
        
        Public Shared JpegOrientationRotateFlips As SortedDictionary(Of Integer, RotateFlipType)
        
        Shared Sub New()
            ImageFormats = New SortedDictionary(Of Guid, String)()
            ImageFormats.Add(ImageFormat.Bmp.Guid, "image/bmp")
            ImageFormats.Add(ImageFormat.Emf.Guid, "image/emf")
            ImageFormats.Add(ImageFormat.Exif.Guid, "image/bmp")
            ImageFormats.Add(ImageFormat.Gif.Guid, "image/gif")
            ImageFormats.Add(ImageFormat.Jpeg.Guid, "image/jpeg")
            ImageFormats.Add(ImageFormat.Png.Guid, "image/png")
            ImageFormats.Add(ImageFormat.Tiff.Guid, "image/tiff")
            ImageFormats.Add(ImageFormat.Wmf.Guid, "image/Wmf")
            JpegOrientationRotateFlips = New SortedDictionary(Of Integer, RotateFlipType)()
            JpegOrientationRotateFlips.Add(1, RotateFlipType.RotateNoneFlipNone)
            JpegOrientationRotateFlips.Add(2, RotateFlipType.RotateNoneFlipX)
            JpegOrientationRotateFlips.Add(3, RotateFlipType.Rotate180FlipNone)
            JpegOrientationRotateFlips.Add(4, RotateFlipType.Rotate180FlipX)
            JpegOrientationRotateFlips.Add(5, RotateFlipType.Rotate90FlipX)
            JpegOrientationRotateFlips.Add(6, RotateFlipType.Rotate90FlipNone)
            JpegOrientationRotateFlips.Add(7, RotateFlipType.Rotate270FlipX)
            JpegOrientationRotateFlips.Add(8, RotateFlipType.Rotate270FlipNone)
        End Sub
        
        ReadOnly Property IHttpHandler_IsReusable() As Boolean Implements IHttpHandler.IsReusable
            Get
                Return false
            End Get
        End Property
        
        Public Shared ReadOnly Property DirectAccessMode() As Boolean
            Get
                Return (Not (BinaryData) Is Nothing)
            End Get
        End Property
        
        Public Shared Property BinaryData() As Byte()
            Get
                Dim o As Object = HttpContext.Current.Items("BlobHandlerInfo_Data")
                If (o Is Nothing) Then
                    Return Nothing
                End If
                Return CType(o,Byte())
            End Get
            Set
                HttpContext.Current.Items("BlobHandlerInfo_Data") = value
            End Set
        End Property
        
        Sub IHttpHandler_ProcessRequest(ByVal context As HttpContext) Implements IHttpHandler.ProcessRequest
            Dim handler As BlobHandlerInfo = BlobHandlerInfo.Current
            If (handler Is Nothing) Then
                Throw New HttpException(404, String.Empty)
            End If
            Dim ba As BlobAdapter = Nothing
            If (Not (handler.DataController) Is Nothing) Then
                ba = BlobAdapterFactory.Create(handler.DataController, handler.FieldName.Replace("""", String.Empty))
            End If
            If (Not (ba) Is Nothing) Then
                handler.ContentTypeField = ba.ContentTypeField
                handler.FileNameField = ba.FileNameField
                handler.LengthField = ba.LengthField
            End If
            Dim val As String = handler.Value.Split(Global.Microsoft.VisualBasic.ChrW(124))(1)
            If ((handler.Mode = BlobMode.Original) OrElse (context.Request.HttpMethod = "POST")) Then
                AppendDownloadTokenCookie()
            End If
            If (handler.Mode = BlobMode.Upload) Then
                RenderUploader(context, handler, handler.SaveFile(context, ba, val))
            Else
                If Blob.DirectAccessMode Then
                    Dim stream As Stream = Nothing
                    If (ba Is Nothing) Then
                        stream = New MemoryStream()
                        handler.LoadFile(stream)
                    Else
                        stream = ba.ReadBlob(val)
                    End If
                    stream.Position = 0
                    Dim data((stream.Length) - 1) As Byte
                    stream.Read(data, 0, data.Length)
                    stream.Close()
                    Blob.BinaryData = data
                    Return
                Else
                    If (ba Is Nothing) Then
                        Dim tempFileName As String = Path.GetTempFileName()
                        Dim stream As Stream = File.Create(tempFileName)
                        handler.LoadFile(stream)
                        CopyToOutput(context, stream, handler)
                        stream.Close()
                        File.Delete(tempFileName)
                    Else
                        Dim key As String = context.Request.Params("_validationKey")
                        If ((Not (ba.IsPublic) AndAlso Not (context.User.Identity.IsAuthenticated)) AndAlso Not (BlobAdapter.ValidationKey.Equals(key))) Then
                            context.Response.StatusCode = 403
                            Return
                        End If
                        Dim stream As Stream = Nothing
                        If handler.Mode.Equals(BlobMode.Thumbnail) Then
                            Dim contentType As String = ba.ReadContentType(val)
                            If (String.IsNullOrEmpty(contentType) OrElse Not (contentType.StartsWith("image/"))) Then
                                stream = New MemoryStream()
                            End If
                        End If
                        If (stream Is Nothing) Then
                            stream = ba.ReadBlob(val)
                        End If
                        handler.ProcessDownloadViaBusinessRule(stream)
                        CopyToOutput(context, stream, handler)
                        If (Not (stream) Is Nothing) Then
                            stream.Close()
                        End If
                    End If
                End If
            End If
            Dim request As HttpRequest = context.Request
            Dim requireCaching As Boolean = (request.IsSecureConnection AndAlso ((request.Browser.Browser = "IE") AndAlso (request.Browser.MajorVersion < 9)))
            If (Not (requireCaching) AndAlso Not ((handler.Mode = BlobMode.Thumbnail))) Then
                context.Response.Cache.SetCacheability(HttpCacheability.NoCache)
            End If
        End Sub
        
        Public Shared Function CreateDataController() As IDataController
            Dim controller As IDataController = ControllerFactory.CreateDataController()
            If DirectAccessMode Then
                CType(controller,DataControllerBase).AllowPublicAccess = true
            End If
            Return controller
        End Function
        
        Public Overloads Shared Function Read(ByVal key As String) As Byte()
            Dim keyInfo() As String = key.Split(New Char() {Global.Microsoft.VisualBasic.ChrW(61)})
            Return Blob.Read(keyInfo(0), keyInfo(1))
        End Function
        
        Public Overloads Shared Function Read(ByVal blobHandler As String, ByVal keyValue As Object) As Byte()
            Dim v As String = keyValue.ToString()
            If Not (v.StartsWith("o|")) Then
                v = ("o|" + v)
            End If
            Dim context As HttpContext = HttpContext.Current
            context.Items("BlobHandlerInfo_Current") = BlobFactory.Handlers(blobHandler)
            context.Items("BlobHandlerInfo_Value") = v
            Blob.BinaryData = New Byte((0) - 1) {}
            CType(New Blob(),IHttpHandler).ProcessRequest(context)
            Dim result() As Byte = Blob.BinaryData
            context.Items.Remove("BlobHandlerInfo_Current")
            context.Items.Remove("BlobHandlerInfo_Value")
            context.Items.Remove("BlobHandlerInfo_Data")
            Return result
        End Function
        
        Public Shared Function ImageFormatToEncoder(ByVal format As ImageFormat) As ImageCodecInfo
            For Each codec As ImageCodecInfo in ImageCodecInfo.GetImageDecoders()
                If (codec.FormatID = format.Guid) Then
                    Return codec
                End If
            Next
            Return Nothing
        End Function
        
        Private Sub CopyToOutput(ByVal context As HttpContext, ByVal stream As Stream, ByVal handler As BlobHandlerInfo)
            Dim offset As Integer = 0
            stream.Position = offset
            Dim buffer() As Byte = Nothing
            Dim img As Image = Nothing
            Dim streamLength As Long = stream.Length
            'attempt to auto-detect content type as an image
            Dim contentType As String = handler.ContentType
            If ((String.IsNullOrEmpty(contentType) OrElse contentType.StartsWith("image/")) AndAlso (stream.Length > 0)) Then
                Try 
                    img = Image.FromStream(stream)
                    If img.RawFormat.Equals(ImageFormat.Jpeg) Then
                        For Each p As PropertyItem in img.PropertyItems
                            If ((p.Id = 274) AndAlso (p.Type = 3)) Then
                                Dim orientation As UShort = BitConverter.ToUInt16(p.Value, 0)
                                Dim flipType As RotateFlipType
                                JpegOrientationRotateFlips.TryGetValue(orientation, flipType)
                                If Not ((flipType = RotateFlipType.RotateNoneFlipNone)) Then
                                    img.RotateFlip(flipType)
                                    img.RemovePropertyItem(p.Id)
                                    stream = New MemoryStream()
                                    Dim saveParams As EncoderParameters = New EncoderParameters()
                                    saveParams.Param(0) = New EncoderParameter(System.Drawing.Imaging.Encoder.Quality, CType(93,UInteger))
                                    img.Save(stream, ImageFormatToEncoder(ImageFormat.Jpeg), saveParams)
                                    streamLength = stream.Length
                                    offset = 0
                                    contentType = "image/jpg"
                                    Exit For
                                End If
                            End If
                        Next
                    End If
                Catch __exception As Exception
                    Try 
                        'Correction for Northwind database image format
                        offset = 78
                        streamLength = (streamLength - 78)
                        stream.Position = offset
                        buffer = New Byte(((stream.Length - offset)) - 1) {}
                        stream.Read(buffer, 0, buffer.Length)
                        img = Image.FromStream(New MemoryStream(buffer, 0, buffer.Length))
                    Catch ex As Exception
                        offset = 0
                        context.Trace.Write(ex.ToString())
                    End Try
                End Try
            End If
            'send an original or a thumbnail to the output
            If (handler.Mode = BlobMode.Thumbnail) Then
                'draw a thumbnail
                Dim thumbWidth As Integer = 92
                Dim thumbHeight As Integer = 64
                Dim crop As Boolean = false
                If ApplicationServices.IsTouchClient Then
                    thumbWidth = 80
                    thumbHeight = 80
                    crop = Not (context.Request.RawUrl.Contains("_nocrop"))
                End If
                Dim thumbnail As Bitmap = New Bitmap(thumbWidth, thumbHeight)
                Dim g As Graphics = Graphics.FromImage(thumbnail)
                Dim r As Rectangle = New Rectangle(0, 0, thumbWidth, thumbHeight)
                g.FillRectangle(Brushes.Transparent, r)
                If (Not (img) Is Nothing) Then
                    Dim thumbnailAspect As Double = (Convert.ToDouble(r.Height) / Convert.ToDouble(r.Width))
                    If ((img.Width < r.Width) AndAlso (img.Height < r.Height)) Then
                        r.Width = img.Width
                        r.Height = img.Height
                    Else
                        If (img.Width > img.Height) Then
                            r.Height = Convert.ToInt32((Convert.ToDouble(r.Width) * thumbnailAspect))
                            r.Width = Convert.ToInt32((Convert.ToDouble(r.Height)  _
                                            * (Convert.ToDouble(img.Width) / Convert.ToDouble(img.Height))))
                        Else
                            If (img.Height > img.Width) Then
                                thumbnailAspect = (Convert.ToDouble(r.Width) / Convert.ToDouble(r.Height))
                                r.Width = Convert.ToInt32((Convert.ToDouble(r.Height) * thumbnailAspect))
                                r.Height = Convert.ToInt32((Convert.ToDouble(r.Width)  _
                                                * (Convert.ToDouble(img.Height) / Convert.ToDouble(img.Width))))
                            Else
                                r.Width = Convert.ToInt32((Convert.ToDouble(img.Height) * thumbnailAspect))
                                r.Height = r.Width
                            End If
                        End If
                    End If
                    Dim aspect As Double = (Convert.ToDouble(thumbnail.Width) / r.Width)
                    If (r.Width <= r.Height) Then
                        aspect = (Convert.ToDouble(thumbnail.Height) / r.Height)
                    End If
                    If (aspect > 1) Then
                        aspect = 1
                    End If
                    r.Width = Convert.ToInt32((Convert.ToDouble(r.Width) * aspect))
                    r.Height = Convert.ToInt32((Convert.ToDouble(r.Height) * aspect))
                    If crop Then
                        If (r.Width > r.Height) Then
                            r.Inflate((r.Width - r.Height), Convert.ToInt32((Convert.ToDouble((r.Width - r.Height)) * aspect)))
                        Else
                            r.Inflate(Convert.ToInt32((Convert.ToDouble((r.Height - r.Width)) * aspect)), (r.Height - r.Width))
                        End If
                    End If
                    r.Location = New Point(((thumbnail.Width - r.Width)  _
                                    / 2), ((thumbnail.Height - r.Height)  _
                                    / 2))
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic
                    g.DrawImage(img, r)
                Else
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit
                    Dim f As Font = New Font("Arial", CType(7.5R,Single))
                    Dim text As String = handler.FileName
                    If String.IsNullOrEmpty(text) Then
                        text = handler.Text
                    Else
                        text = Path.GetExtension(text)
                        If (text.StartsWith(".") AndAlso (text.Length > 1)) Then
                            text = text.Substring(1).ToLower()
                            f = New Font("Arial", CType(12,Single), FontStyle.Bold)
                        End If
                    End If
                    g.FillRectangle(Brushes.White, r)
                    g.DrawString(text, f, Brushes.Black, r)
                End If
                'produce thumbnail data
                Dim ts As MemoryStream = New MemoryStream()
                thumbnail.Save(ts, ImageFormat.Png)
                ts.Flush()
                ts.Position = 0
                Dim td((ts.Length) - 1) As Byte
                ts.Read(td, 0, td.Length)
                ts.Close()
                'Send thumbnail to the output
                context.Response.AddHeader("Content-Length", td.Length.ToString())
                context.Response.ContentType = "image/png"
                context.Response.OutputStream.Write(td, 0, td.Length)
                If ((img Is Nothing) AndAlso (handler.Mode = BlobMode.Original)) Then
                    context.Response.Cache.SetCacheability(HttpCacheability.NoCache)
                Else
                    context.Response.Cache.SetCacheability(HttpCacheability.Public)
                    context.Response.Cache.SetExpires(DateTime.Now.AddMinutes(Blob.ThumbnailCacheTimeout))
                End If
            Else
                If ((Not (img) Is Nothing) AndAlso String.IsNullOrEmpty(contentType)) Then
                    contentType = ImageFormats(img.RawFormat.Guid)
                End If
                If String.IsNullOrEmpty(contentType) Then
                    contentType = "application/octet-stream"
                End If
                Dim fileName As String = handler.FileName
                If String.IsNullOrEmpty(fileName) Then
                    fileName = String.Format("{0}{1}.{2}", handler.Key, handler.Reference, contentType.Substring((contentType.IndexOf("/") + 1)))
                End If
                context.Response.ContentType = contentType
                context.Response.AddHeader("Content-Disposition", ("filename=" + HttpUtility.UrlEncode(fileName)))
                context.Response.AddHeader("Content-Length", streamLength.ToString())
                If (stream.Length = 0) Then
                    context.Response.StatusCode = 404
                    Return
                End If
                stream.Position = offset
                buffer = New Byte(((1024 * 32)) - 1) {}
                Dim bytesRead As Integer = stream.Read(buffer, 0, buffer.Length)
                Do While (bytesRead > 0)
                    context.Response.OutputStream.Write(buffer, 0, bytesRead)
                    offset = (offset + bytesRead)
                    bytesRead = stream.Read(buffer, 0, buffer.Length)
                Loop
            End If
        End Sub
        
        Private Sub RenderUploader(ByVal context As HttpContext, ByVal handler As BlobHandlerInfo, ByVal uploadSuccess As Boolean)
            Dim writer As HtmlTextWriter = New HtmlTextWriter(context.Response.Output)
            writer.WriteLine("<!DOCTYPE html PUBLIC \""-//W3C//DTD XHTML 1.0 Transitional//EN\"" \""http://www.w3."& _ 
                    "org/TR/xhtml1/DTD/xhtml1-transitional.dtd\"">")
            writer.AddAttribute("xmlns", "http://www.w3.org/1999/xhtml")
            writer.RenderBeginTag(HtmlTextWriterTag.Html)
            'head
            writer.RenderBeginTag(HtmlTextWriterTag.Head)
            writer.RenderBeginTag(HtmlTextWriterTag.Title)
            writer.Write("Uploader")
            writer.RenderEndTag()
            writer.AddAttribute(HtmlTextWriterAttribute.Type, "text/javascript")
            writer.RenderBeginTag(HtmlTextWriterTag.Script)
            Dim script As String = "" & ControlChars.CrLf &"                       " & ControlChars.CrLf &"function ShowUploadControls() { " & ControlChars.CrLf &"    document.getElem"& _ 
                "entById('UploadControlsPanel').style.display ='block'; " & ControlChars.CrLf &"    document.getElement"& _ 
                "ById('StartUploadPanel').style.display = 'none';   " & ControlChars.CrLf &"    document.getElementById"& _ 
                "('FileUpload').focus();      " & ControlChars.CrLf &"} " & ControlChars.CrLf &"function Owner() {" & ControlChars.CrLf &"    var m = window.locati"& _ 
                "on.href.match(/owner=(.+?)&/);" & ControlChars.CrLf &"    return m ? parent.$find(m[1]) : null;" & ControlChars.CrLf &"}" & ControlChars.CrLf &"fu"& _ 
                "nction StartUpload(msg) {" & ControlChars.CrLf &"    if (msg && !window.confirm(msg)) return;" & ControlChars.CrLf &"    if "& _ 
                "(parent && parent.window.Web) {" & ControlChars.CrLf &"        var m = window.location.href.match(/&in"& _ 
                "dex=(\d+)$/);" & ControlChars.CrLf &"        if (m) Owner()._showUploadProgress(m[1], document.forms[0"& _ 
                "]);" & ControlChars.CrLf &"    }" & ControlChars.CrLf &"}" & ControlChars.CrLf &"function UploadSuccess(key, message) { " & ControlChars.CrLf &"    if (!Owner().get_isI"& _ 
                "nserting())" & ControlChars.CrLf &"        if (parent && parent.window.Web) { " & ControlChars.CrLf &"            parent.Web"& _ 
                ".DataView.showMessage(message); " & ControlChars.CrLf &"            Owner().refresh(false,null,'FIELD_"& _ 
                "NAME');" & ControlChars.CrLf &"        }     " & ControlChars.CrLf &"        else " & ControlChars.CrLf &"            alert('Success');" & ControlChars.CrLf &"}"
            writer.WriteLine(script.Replace("FIELD_NAME", String.Format("^({0}|{1}|{2})?$", handler.ContentTypeField, handler.FileNameField, handler.LengthField)))
            writer.RenderEndTag()
            writer.AddAttribute(HtmlTextWriterAttribute.Type, "text/css")
            writer.RenderBeginTag(HtmlTextWriterTag.Style)
            writer.WriteLine("body{font-family:tahoma;font-size:8.5pt;margin:4px;background-color:white;}")
            writer.WriteLine("input{font-family:tahoma;font-size:8.5pt;}")
            writer.WriteLine("input.FileUpload{padding:3px}")
            writer.RenderEndTag()
            writer.RenderEndTag()
            'body
            Dim message As String = Nothing
            If uploadSuccess Then
                If (HttpContext.Current.Request.Files(0).ContentLength > 0) Then
                    message = String.Format(Localizer.Replace("BlobUploded", "<b>Confirmation:</b> {0} has been uploaded successfully. <b>It may take up to {1}"& _ 
                                " minutes for the thumbnail to reflect the uploaded content.</b>"), handler.Text.ToLower(), Blob.ThumbnailCacheTimeout)
                Else
                    message = String.Format(Localizer.Replace("BlobCleared", "<b>Confirmation:</b> {0} has been cleared."), handler.Text.ToLower())
                End If
            Else
                If Not (String.IsNullOrEmpty(handler.Error)) Then
                    message = String.Format(Localizer.Replace("BlobUploadError", "<b>Error:</b> failed to upload {0}. {1}"), handler.Text.ToLower(), BusinessRules.JavaScriptString(handler.Error))
                End If
            End If
            If Not (String.IsNullOrEmpty(message)) Then
                writer.AddAttribute("onload", String.Format("UploadSuccess('{0}={1}', '{2}')", handler.Key, handler.Value.Replace("u|", "t|"), BusinessRules.JavaScriptString(message)))
            End If
            writer.RenderBeginTag(HtmlTextWriterTag.Body)
            'form
            writer.AddAttribute(HtmlTextWriterAttribute.Name, "form1")
            writer.AddAttribute("method", "post")
            writer.AddAttribute("action", context.Request.RawUrl)
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "form1")
            writer.AddAttribute("enctype", "multipart/form-data")
            writer.RenderBeginTag(HtmlTextWriterTag.Form)
            writer.RenderBeginTag(HtmlTextWriterTag.Div)
            'begin "start upload" controls
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "StartUploadPanel")
            writer.RenderBeginTag(HtmlTextWriterTag.Div)
            writer.Write(Localizer.Replace("BlobUploadLinkPart1", "Click"))
            writer.Write(" ")
            writer.AddAttribute(HtmlTextWriterAttribute.Href, "#")
            writer.AddAttribute(HtmlTextWriterAttribute.Onclick, "ShowUploadControls();return false")
            writer.RenderBeginTag(HtmlTextWriterTag.A)
            writer.Write(Localizer.Replace("BlobUploadLinkPart2", "here"))
            writer.RenderEndTag()
            writer.Write(" ")
            writer.Write(Localizer.Replace("BlobUploadLinkPart3", "to upload or clear {0} file."), handler.Text.ToLower())
            'end of "start upload" controls
            writer.RenderEndTag()
            'begin "upload controls"
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "UploadControlsPanel")
            writer.AddAttribute(HtmlTextWriterAttribute.Style, "display:none")
            writer.RenderBeginTag(HtmlTextWriterTag.Div)
            '"FileUpload" input
            writer.AddAttribute(HtmlTextWriterAttribute.Type, "File")
            writer.AddAttribute(HtmlTextWriterAttribute.Name, "FileUpload")
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "FileUpload")
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "FileUpload")
            writer.AddAttribute(HtmlTextWriterAttribute.Onchange, "StartUpload()")
            writer.RenderBeginTag(HtmlTextWriterTag.Input)
            writer.RenderEndTag()
            '"FileClear" input
            If Not ((context.Request.QueryString(handler.Key) = "u|")) Then
                writer.AddAttribute(HtmlTextWriterAttribute.Type, "button")
                writer.AddAttribute(HtmlTextWriterAttribute.Id, "FileClear")
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "FileClear")
                writer.AddAttribute(HtmlTextWriterAttribute.Onclick, String.Format("StartUpload('{0}')", BusinessRules.JavaScriptString(Localizer.Replace("BlobClearConfirm", "Clear?"))))
                writer.AddAttribute(HtmlTextWriterAttribute.Value, Localizer.Replace("BlobClearText", "Clear"))
                writer.RenderBeginTag(HtmlTextWriterTag.Input)
                writer.RenderEndTag()
            End If
            'end of "upload controls"
            writer.RenderEndTag()
            'close "div"
            writer.RenderEndTag()
            'close "form"
            writer.RenderEndTag()
            'close "body"
            writer.RenderEndTag()
            'close "html"
            writer.RenderEndTag()
            writer.Close()
        End Sub
    End Class
    
    Public Class BlobAdapterArguments
        Inherits SortedDictionary(Of String, String)
    End Class
    
    Public Class BlobAdapterFactoryBase
        
        Public Shared ArgumentParserRegex As Regex = New Regex("^\s*(?'ArgumentName'[\w\-]+)\s*:\s*(?'ArgumentValue'[\s\S]+?)\s*$", (RegexOptions.Multiline Or RegexOptions.IgnoreCase))
        
        Protected Overridable Function ParseAdapterConfig(ByVal fieldName As String, ByVal config As String) As BlobAdapterArguments
            Dim capture As Boolean = false
            Dim args As BlobAdapterArguments = New BlobAdapterArguments()
            Dim m As Match = ArgumentParserRegex.Match(config)
            Do While m.Success
                Dim name As String = m.Groups("ArgumentName").Value.ToLower()
                Dim value As String = m.Groups("ArgumentValue").Value
                If name.Equals("field") Then
                    capture = (fieldName = value)
                End If
                If capture Then
                    args(name) = value
                End If
                m = m.NextMatch()
            Loop
            Return args
        End Function
        
        Protected Overridable Function CreateFromConfig(ByVal controller As String, ByVal fieldName As String, ByVal adapterConfig As String) As BlobAdapter
            If Not (adapterConfig.Contains(fieldName)) Then
                Return Nothing
            End If
            Dim arguments As BlobAdapterArguments = ParseAdapterConfig(fieldName, adapterConfig)
            If arguments.Count.Equals(0) Then
                Return Nothing
            End If
            Try 
                Dim storageSystem As String = arguments("storage-system").ToLower()
                If (storageSystem = "file") Then
                    Return New FileSystemBlobAdapter(controller, arguments)
                End If
                If (storageSystem = "azure") Then
                    Return New AzureBlobAdapter(controller, arguments)
                End If
                If (storageSystem = "s3") Then
                    Return New S3BlobAdapter(controller, arguments)
                End If
            Catch __exception As Exception
            End Try
            Return Nothing
        End Function
        
        Protected Shared Function ReadConfig(ByVal controller As String) As String
            Dim config As ControllerConfiguration = DataControllerBase.CreateConfigurationInstance(GetType(BlobAdapter), controller)
            Return CType(config.Evaluate("string(/c:dataController/c:blobAdapterConfig)"),String).Trim()
        End Function
        
        Public Shared Function Create(ByVal controller As String, ByVal fieldName As String) As BlobAdapter
            Dim adapterConfig As String = ReadConfig(controller)
            If String.IsNullOrEmpty(adapterConfig) Then
                Return Nothing
            End If
            Dim factory As BlobAdapterFactory = New BlobAdapterFactory()
            Return factory.CreateFromConfig(controller, fieldName, adapterConfig)
        End Function
        
        Public Shared Sub InitializeRow(ByVal page As ViewPage, ByVal row() As Object)
            Dim adapterConfig As String = ReadConfig(page.Controller)
            If String.IsNullOrEmpty(adapterConfig) Then
                Return
            End If
            Dim factory As BlobAdapterFactory = New BlobAdapterFactory()
            Dim blobFieldIndex As Integer = 0
            For Each field As DataField in page.Fields
                Dim ba As BlobAdapter = factory.CreateFromConfig(page.Controller, field.Name, adapterConfig)
                If (Not (ba) Is Nothing) Then
                    Dim pk As Object = Nothing
                    Dim primaryKeyFieldIndex As Integer = 0
                    For Each keyField As DataField in page.Fields
                        If keyField.IsPrimaryKey Then
                            pk = row(primaryKeyFieldIndex)
                            If ((Not (pk) Is Nothing) AndAlso TypeOf pk Is Byte) Then
                                pk = New Guid(CType(pk,Byte()))
                            End If
                            Exit For
                        End If
                        primaryKeyFieldIndex = (primaryKeyFieldIndex + 1)
                    Next
                    Dim utilityFieldIndex As Integer = 0
                    Dim fileName As String = String.Empty
                    Dim contentType As String = String.Empty
                    Dim length As Integer = -1
                    For Each utilityField As DataField in page.Fields
                        If (utilityField.Name = ba.FileNameField) Then
                            fileName = Convert.ToString(row(utilityFieldIndex))
                        Else
                            If (utilityField.Name = ba.ContentTypeField) Then
                                contentType = Convert.ToString(row(utilityFieldIndex))
                            Else
                                If (utilityField.Name = ba.LengthField) Then
                                    length = Convert.ToInt32(row(utilityFieldIndex))
                                End If
                            End If
                        End If
                        utilityFieldIndex = (utilityFieldIndex + 1)
                    Next
                    If (Not ((length = 0)) AndAlso (Not (String.IsNullOrEmpty(fileName)) OrElse Not (String.IsNullOrEmpty(contentType)))) Then
                        row(blobFieldIndex) = pk.ToString()
                    End If
                End If
                blobFieldIndex = (blobFieldIndex + 1)
            Next
        End Sub
    End Class
    
    Partial Public Class BlobAdapterFactory
        Inherits BlobAdapterFactoryBase
    End Class
    
    Public Class BlobAdapter
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Controller As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_FieldName As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Arguments As BlobAdapterArguments
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_PathTemplate As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ContentTypeField As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_LengthField As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_FileNameField As String
        
        Protected _page As ViewPage
        
        Protected _keyValue As String
        
        Public Sub New(ByVal controller As String, ByVal arguments As BlobAdapterArguments)
            MyBase.New
            Me.Controller = controller
            Me.Arguments = arguments
            Initialize()
        End Sub
        
        Public Property Controller() As String
            Get
                Return Me.m_Controller
            End Get
            Set
                Me.m_Controller = value
            End Set
        End Property
        
        Public Property FieldName() As String
            Get
                Return Me.m_FieldName
            End Get
            Set
                Me.m_FieldName = value
            End Set
        End Property
        
        Public Property Arguments() As BlobAdapterArguments
            Get
                Return Me.m_Arguments
            End Get
            Set
                Me.m_Arguments = value
            End Set
        End Property
        
        Public Property PathTemplate() As String
            Get
                Return Me.m_PathTemplate
            End Get
            Set
                Me.m_PathTemplate = value
            End Set
        End Property
        
        Public Property ContentTypeField() As String
            Get
                Return Me.m_ContentTypeField
            End Get
            Set
                Me.m_ContentTypeField = value
            End Set
        End Property
        
        Public Property LengthField() As String
            Get
                Return Me.m_LengthField
            End Get
            Set
                Me.m_LengthField = value
            End Set
        End Property
        
        Public Property FileNameField() As String
            Get
                Return Me.m_FileNameField
            End Get
            Set
                Me.m_FileNameField = value
            End Set
        End Property
        
        Public Shared ReadOnly Property ValidationKey() As String
            Get
                Return "95AF92142A27408C79680310B7893C9E7555E6E9FE413C5C551446B86655F08498BE45114E264DD3B"& _ 
                    "2432D296FBCB11A97E3167991FA739BB72D6C3D5CEE56F2"
            End Get
        End Property
        
        Public Overridable ReadOnly Property IsPublic() As Boolean
            Get
                Return false
            End Get
        End Property
        
        Protected Overridable Sub Initialize()
            Me.FieldName = Arguments("field")
            Dim s As String = Nothing
            If Arguments.TryGetValue("path-template", s) Then
                Me.PathTemplate = s
            End If
            If Arguments.TryGetValue("content-type-field", s) Then
                Me.ContentTypeField = s
            Else
                Me.ContentTypeField = (FieldName + "ContentType")
            End If
            If Arguments.TryGetValue("length-field", s) Then
                Me.LengthField = s
            Else
                Me.LengthField = (FieldName + "Length")
            End If
            If Arguments.TryGetValue("file-name-field", s) Then
                Me.FileNameField = s
            Else
                Me.FileNameField = (FieldName + "FileName")
            End If
        End Sub
        
        Public Overridable Function ReadBlob(ByVal keyValue As String) As Stream
            Return Nothing
        End Function
        
        Public Overridable Function WriteBlob(ByVal file As HttpPostedFile, ByVal keyValue As String) As Boolean
            Return false
        End Function
        
        Public Overridable Function SelectViewPageByKey(ByVal keyValue As String) As ViewPage
            Dim config As ControllerConfiguration = DataControllerBase.CreateConfigurationInstance(GetType(BlobAdapter), Me.Controller)
            Dim keyField As String = CType(config.Evaluate("string(/c:dataController/c:fields/c:field[@isPrimaryKey='true']/@name)"),String)
            Dim request As PageRequest = New PageRequest()
            request.Controller = Controller
            request.View = DataControllerBase.GetSelectView(Controller)
            request.Filter = New String() {String.Format("{0}:={1}", keyField, keyValue)}
            request.RequiresMetaData = true
            request.PageSize = 1
            Dim page As ViewPage = Blob.CreateDataController().GetPage(request.Controller, request.View, request)
            Return page
        End Function
        
        Public Overridable Sub CopyData(ByVal input As Stream, ByVal output As Stream)
            Dim buffer(((16 * 1024)) - 1) As Byte
            Dim bytesRead As Integer
            Dim readNext As Boolean = true
            Do While readNext
                bytesRead = input.Read(buffer, 0, buffer.Length)
                output.Write(buffer, 0, bytesRead)
                If (bytesRead = 0) Then
                    readNext = false
                End If
            Loop
        End Sub
        
        Public Function KeyValueToPath(ByVal keyValue As String) As String
            Dim extendedPath As String = ExtendPathTemplate(keyValue)
            If extendedPath.StartsWith("/") Then
                extendedPath = extendedPath.Substring(1)
            End If
            Return extendedPath
        End Function
        
        Public Overloads Overridable Function ExtendPathTemplate(ByVal keyValue As String) As String
            Return ExtendPathTemplate(PathTemplate, keyValue)
        End Function
        
        Public Overloads Overridable Function ExtendPathTemplate(ByVal template As String, ByVal keyValue As String) As String
            If (String.IsNullOrEmpty(template) OrElse Not (template.Contains("{"))) Then
                Return keyValue
            End If
            _keyValue = keyValue
            Dim extendedPath As String = Regex.Replace(template, "\{(\$?\w+)\}", AddressOf DoReplaceFieldNameInTemplate)
            If extendedPath.StartsWith("~") Then
                extendedPath = extendedPath.Substring(1)
                If extendedPath.StartsWith("\") Then
                    extendedPath = extendedPath.Substring(1)
                End If
                extendedPath = Path.Combine(HttpRuntime.AppDomainAppPath, extendedPath)
            End If
            Return extendedPath
        End Function
        
        Protected Overridable Function DoReplaceFieldNameInTemplate(ByVal m As Match) As String
            If (Me._page Is Nothing) Then
                Me._page = SelectViewPageByKey(Me._keyValue)
            End If
            Dim fieldIndex As Integer = 0
            Dim targetFieldName As String = m.Groups(1).Value
            Dim fieldName As String = targetFieldName
            Dim requiresProcessing As Boolean = fieldName.StartsWith("$")
            If requiresProcessing Then
                fieldName = Me.FileNameField
            End If
            For Each df As DataField in Me._page.Fields
                If (df.Name = fieldName) Then
                    Dim v As String = Convert.ToString(Me._page.Rows(0)(fieldIndex))
                    If requiresProcessing Then
                        If targetFieldName.Equals("$Extension", StringComparison.OrdinalIgnoreCase) Then
                            Dim extension As String = Path.GetExtension(v)
                            If extension.StartsWith(".") Then
                                extension = extension.Substring(1)
                            End If
                            Return extension
                        End If
                        If targetFieldName.Equals("$FileNameWithoutExtension", StringComparison.OrdinalIgnoreCase) Then
                            Return Path.GetFileNameWithoutExtension(v)
                        End If
                    End If
                    Return v
                End If
                fieldIndex = (fieldIndex + 1)
            Next
            Return String.Empty
        End Function
        
        Public Overridable Sub ValidateFieldValue(ByVal fv As FieldValue)
            If ((fv.Name = FileNameField) AndAlso fv.Modified) Then
                Dim newValue As String = Convert.ToString(fv.NewValue)
                If Not (String.IsNullOrEmpty(newValue)) Then
                    fv.NewValue = Regex.Replace(newValue, "[^\w\.]", "-")
                End If
            End If
        End Sub
        
        Public Overridable Function ReadContentType(ByVal keyValue As String) As String
            Return ExtendPathTemplate(String.Format("{{{0}}}", ContentTypeField), keyValue)
        End Function
    End Class
End Namespace

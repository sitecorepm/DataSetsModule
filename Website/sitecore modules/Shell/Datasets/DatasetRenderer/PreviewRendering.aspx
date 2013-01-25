<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="PreviewRendering.aspx.cs" Inherits="Sitecore.SharedSource.Dataset.UI.DatasetRenderer.PreviewRendering" %>
<%@ Register Namespace="Sitecore.SharedSource.Dataset.ServerControls" Assembly="Sitecore.SharedSource.Dataset.ServerControls" TagPrefix="ds" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title></title>
    <link href="Preview.css" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <ds:DatasetRenderer ID="dsRenderer" runat="server"></ds:DatasetRenderer>
    </div>
    </form>
</body>
</html>

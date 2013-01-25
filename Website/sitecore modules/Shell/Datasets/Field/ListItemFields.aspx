<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ListItemFields.aspx.cs" Inherits="Sitecore.SharedSource.Dataset.UI.DatasetRenderer.ListItemFields" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div style="padding: 2px 2px 2px 2px;">
        <asp:Repeater ID="rptrFields" runat="server">
            <ItemTemplate>
                <span style="white-space: nowrap; cursor:pointer; float:left; font: 10pt Verdana; border: solid 1px #CAD8F3; background-color: #DEE7F8; border: solid 1px #999999; padding: 0px 2px 0px 2px; margin: 1px 2px 1px 2px" onclick="window.clipboardData.setData('Text',this.innerText);">
                    [<%# Container.DataItem %>]
                </span>
            </ItemTemplate>
        </asp:Repeater>
    </div>
    </form>
</body>
</html>

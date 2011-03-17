<?xml version="1.0" encoding="ISO-8859-1"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:mt="urn:mtconnect.org:MTConnectStreams:1.1" xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" exclude-result-prefixes="mt">
	<xsl:template match="/">
		<html>
			<body>
				<table border="1">
					<tr bgcolor="#9acd32">
						<th>Id</th>
						<th>Name</th>
						<th>Value</th>
					</tr>
					<xsl:for-each select="//mt:Position">
						<tr>
							<td>
								<xsl:value-of select="@dataItemId"/>
							</td>
							<td>
								<xsl:value-of select="@name"/>
							</td>
							<td>
								<xsl:value-of select="//mt:Position"/>
							</td>
						</tr>
					</xsl:for-each>
				</table>
			</body>
		</html>
	</xsl:template>
</xsl:stylesheet>

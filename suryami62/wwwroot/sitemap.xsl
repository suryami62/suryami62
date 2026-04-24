<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:s="http://www.sitemaps.org/schemas/sitemap/0.9"
                version="1.0">
    <xsl:output method="html" indent="yes" encoding="UTF-8"/>

    <xsl:template match="/">
        <html>
            <head>
                <meta charset="utf-8"/>
                <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
                <title>XML Sitemap</title>
                <style>
                    /* Neo-Brutalist Design System */
                    :root {
                    --accent: #a3e635;
                    --black: #000000;
                    --white: #ffffff;
                    --shadow: 8px 8px 0px 0px #000000;
                    }

                    @media (prefers-color-scheme: dark) {
                    :root {
                    --bg: #1a1a1a;
                    --text: #ffffff;
                    --shadow: 8px 8px 0px 0px #ffffff;
                    --table-header-bg: #2a2a2a;
                    --table-row-bg: #1a1a1a;
                    --table-row-alt: #252525;
                    --border: #ffffff;
                    }
                    }

                    @media (prefers-color-scheme: light) {
                    :root {
                    --bg: #ffffff;
                    --text: #000000;
                    --shadow: 8px 8px 0px 0px #000000;
                    --table-header-bg: #f5f5f5;
                    --table-row-bg: #ffffff;
                    --table-row-alt: #f9f9f9;
                    --border: #000000;
                    }
                    }

                    * {
                    box-sizing: border-box;
                    }

                    body {
                    font-family: 'Helvetica Neue', Arial, sans-serif;
                    background-color: var(--bg, #ffffff);
                    color: var(--text, #000000);
                    margin: 0;
                    padding: 2rem;
                    min-height: 100vh;
                    }

                    .container {
                    max-width: 1200px;
                    margin: 0 auto;
                    }

                    /* Header Card */
                    .header {
                    background-color: var(--accent, #a3e635);
                    border: 3px solid var(--border, #000000);
                    box-shadow: var(--shadow, 8px 8px 0px 0px #000000);
                    padding: 1.5rem 2rem;
                    margin-bottom: 2rem;
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                    flex-wrap: wrap;
                    gap: 1rem;
                    }

                    .header h1 {
                    margin: 0;
                    font-size: 1.75rem;
                    font-weight: 800;
                    text-transform: uppercase;
                    letter-spacing: -0.02em;
                    }

                    .url-count {
                    background-color: var(--black, #000000);
                    color: var(--accent, #a3e635);
                    border: 2px solid var(--border, #000000);
                    padding: 0.5rem 1rem;
                    font-weight: 700;
                    font-size: 0.875rem;
                    text-transform: uppercase;
                    }

                    /* Table Container */
                    .table-container {
                    background-color: var(--bg, #ffffff);
                    border: 3px solid var(--border, #000000);
                    box-shadow: var(--shadow, 8px 8px 0px 0px #000000);
                    overflow-x: auto;
                    }

                    table {
                    width: 100%;
                    border-collapse: collapse;
                    margin: 0;
                    }

                    /* Table Header */
                    thead {
                    background-color: var(--table-header-bg, #f5f5f5);
                    border-bottom: 3px solid var(--border, #000000);
                    }

                    th {
                    padding: 1rem 1.5rem;
                    text-align: left;
                    font-weight: 800;
                    font-size: 0.75rem;
                    text-transform: uppercase;
                    letter-spacing: 0.05em;
                    border-right: 2px solid var(--border, #000000);
                    }

                    th:last-child {
                    border-right: none;
                    }

                    /* Table Body */
                    tbody tr {
                    border-bottom: 2px solid var(--border, #000000);
                    }

                    tbody tr:last-child {
                    border-bottom: none;
                    }

                    tbody tr:nth-child(even) {
                    background-color: var(--table-row-alt, #f9f9f9);
                    }

                    td {
                    padding: 1rem 1.5rem;
                    border-right: 2px solid var(--border, #000000);
                    }

                    td:last-child {
                    border-right: none;
                    }

                    /* URL Link */
                    .url-link {
                    color: var(--text, #000000);
                    text-decoration: none;
                    font-weight: 500;
                    font-family: 'Courier New', monospace;
                    font-size: 0.875rem;
                    word-break: break-all;
                    }

                    .url-link:hover {
                    background-color: var(--accent, #a3e635);
                    padding: 0.25rem 0.5rem;
                    margin: -0.25rem -0.5rem;
                    border: 2px solid var(--border, #000000);
                    }

                    /* Last Modified Date */
                    .lastmod {
                    font-family: 'Courier New', monospace;
                    font-size: 0.875rem;
                    color: var(--text, #000000);
                    opacity: 0.8;
                    }

                    /* Empty State */
                    .empty-state {
                    text-align: center;
                    padding: 3rem;
                    font-weight: 700;
                    text-transform: uppercase;
                    }

                    /* Responsive */
                    @media (max-width: 768px) {
                    body {
                    padding: 1rem;
                    }

                    .header {
                    flex-direction: column;
                    text-align: center;
                    }

                    .header h1 {
                    font-size: 1.25rem;
                    }

                    th, td {
                    padding: 0.75rem 1rem;
                    }
                    }
                </style>
            </head>
            <body>
                <div class="container">
                    <div class="header">
                        <h1>📋 XML Sitemap</h1>
                        <div class="url-count">
                            <xsl:value-of select="count(s:urlset/s:url)"/> URLs
                        </div>
                    </div>

                    <div class="table-container">
                        <table>
                            <thead>
                                <tr>
                                    <th>URL</th>
                                    <th>Last Modified</th>
                                </tr>
                            </thead>
                            <tbody>
                                <xsl:choose>
                                    <xsl:when test="count(s:urlset/s:url) > 0">
                                        <xsl:for-each select="s:urlset/s:url">
                                            <tr>
                                                <td>
                                                    <a class="url-link" href="{s:loc}">
                                                        <xsl:value-of select="s:loc"/>
                                                    </a>
                                                </td>
                                                <td class="lastmod">
                                                    <xsl:choose>
                                                        <xsl:when test="s:lastmod">
                                                            <xsl:value-of select="s:lastmod"/>
                                                        </xsl:when>
                                                        <xsl:otherwise>—</xsl:otherwise>
                                                    </xsl:choose>
                                                </td>
                                            </tr>
                                        </xsl:for-each>
                                    </xsl:when>
                                    <xsl:otherwise>
                                        <tr>
                                            <td colspan="2" class="empty-state">No URLs found in sitemap</td>
                                        </tr>
                                    </xsl:otherwise>
                                </xsl:choose>
                            </tbody>
                        </table>
                    </div>
                </div>
            </body>
        </html>
    </xsl:template>
</xsl:stylesheet>

<?xml version="1.0" encoding="utf-8"?>
<!--
  Tampilan untuk sitemap.xml. XSLT 1.0 dijalankan browser, bukan Hugo.
  CSS sengaja ditulis di sini (salinan kecil dari main.css) karena file ini berdiri sendiri
  dan main.css punya nama berhash yang berubah tiap build.
-->
<xsl:stylesheet version="1.0"
  xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:sm="http://www.sitemaps.org/schemas/sitemap/0.9"
  exclude-result-prefixes="sm">

  <xsl:output method="html" version="5.0" encoding="UTF-8" indent="yes"
              doctype-system="about:legacy-compat"/>

  <xsl:template match="/">
    <html lang="en">
      <head>
        <meta charset="utf-8"/>
        <meta name="viewport" content="width=device-width, initial-scale=1"/>
        <meta name="color-scheme" content="light dark"/>
        <title>Sitemap | suryami62</title>
        <style>
          :root { color-scheme: light dark; --fg: light-dark(#000, #fff); --bg: light-dark(#fff, #000); --muted: light-dark(#525252, #a3a3a3); --soft: light-dark(#d4d4d4, #404040); }
          * { box-sizing: border-box; }
          body { margin: 0; min-height: 100vh; background: var(--bg); color: var(--fg); font-family: ui-sans-serif, system-ui, sans-serif; line-height: 1.5; }
          .wrap { max-width: 60rem; margin-inline: auto; }
          header { padding: 1rem; border-bottom: 4px solid var(--fg); }
          .brand { display: inline-block; padding: 0.25rem 0.75rem; background: var(--fg); color: var(--bg); box-shadow: 4px 4px 0 var(--fg); font-size: 1.5rem; font-weight: 900; letter-spacing: -0.05em; text-decoration: none; text-transform: uppercase; transform: rotate(-2deg); }
          main { padding: 3rem 1rem; }
          h1 { margin: 0; font-size: clamp(3rem, 9vw, 6rem); font-weight: 900; line-height: 1; letter-spacing: -0.05em; text-transform: uppercase; }
          .lead { max-width: 42rem; margin: 1.5rem 0 0; font-size: 1.25rem; font-weight: 700; }
          .muted { margin: 0.5rem 0 0; color: var(--muted); font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace; font-size: 0.875rem; }
          .card { margin-top: 2rem; border: 4px solid var(--fg); box-shadow: 8px 8px 0 var(--fg); }
          table { width: 100%; border-collapse: collapse; }
          th, td { padding: 0.75rem 1rem; border-bottom: 2px solid var(--fg); text-align: left; overflow-wrap: anywhere; }
          th { background: var(--fg); color: var(--bg); font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace; font-size: 0.875rem; letter-spacing: 0.1em; text-transform: uppercase; }
          tbody tr:last-child td { border-bottom: 0; }
          tbody tr:hover { background: var(--soft); }
          td:last-child { white-space: nowrap; font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace; }
          a { color: inherit; font-weight: 700; }
          a:focus-visible { outline: 3px solid var(--fg); outline-offset: 3px; }
          .sr-only { position: absolute; width: 1px; height: 1px; margin: -1px; overflow: hidden; clip-path: inset(50%); white-space: nowrap; }
        </style>
      </head>
      <body>
        <header>
          <div class="wrap"><a class="brand" href="/">suryami62</a></div>
        </header>
        <main class="wrap">
          <h1>Sitemap</h1>
          <p class="lead">This file is written for search engines. Humans get this view instead.</p>
          <p class="muted">Pages listed: <xsl:value-of select="count(sm:urlset/sm:url)"/></p>
          <div class="card">
            <table>
              <caption class="sr-only">Pages listed in this sitemap</caption>
              <thead>
                <tr><th scope="col">URL</th><th scope="col">Last modified</th></tr>
              </thead>
              <tbody>
                <xsl:for-each select="sm:urlset/sm:url">
                  <tr>
                    <td><a href="{sm:loc}"><xsl:value-of select="sm:loc"/></a></td>
                    <td>
                      <xsl:choose>
                        <xsl:when test="sm:lastmod">
                          <time datetime="{sm:lastmod}"><xsl:value-of select="substring(sm:lastmod, 1, 10)"/></time>
                        </xsl:when>
                        <xsl:otherwise>not set</xsl:otherwise>
                      </xsl:choose>
                    </td>
                  </tr>
                </xsl:for-each>
              </tbody>
            </table>
          </div>
        </main>
      </body>
    </html>
  </xsl:template>
</xsl:stylesheet>

/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ["./**/*.{html,razor}"],
  theme: {
    extend: {},
  },
  plugins: [
    require('@tailwindcss/typography'),
    require('daisyui'),
  ],
  daisyui: {
    themes: [
      {
        dim: {
          ...require("daisyui/src/theming/themes")["dim"],
          "--tab-border": "4px",
        },
        light: {
          ...require("daisyui/src/theming/themes")["light"],
          "--tab-border": "4px",
        },
      },
    ],
  },
}

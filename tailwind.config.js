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
        garden: {
          ...require("daisyui/src/theming/themes")["garden"],
          "--tab-border": "4px",
        },
        dim: {
          ...require("daisyui/src/theming/themes")["dim"],
          "--tab-border": "4px",
        },
      },
    ],
  },
}

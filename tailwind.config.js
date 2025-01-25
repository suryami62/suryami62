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
                    ".brand": {
                        "fill": "#000",
                    },
                },
            },
            {
                dim: {
                    ...require("daisyui/src/theming/themes")["dim"],
                    ".brand": {
                        "fill": "#fff",
                    },
                },
            },
        ],
    },
}

/** @type {import('tailwindcss').Config} */
module.exports = {
    content: ["./**/*.{razor,html,cshtml}"],
    daisyui: {
        themes: ["light"],
    },
    theme: {
        extend: {},
    },
    plugins: [
        require('@tailwindcss/typography'),
        require('@tailwindcss/forms')({
            strategy: 'class',
        }),
        require('@tailwindcss/aspect-ratio'),
        require('@tailwindcss/container-queries'),
        require("daisyui")
    ],
}


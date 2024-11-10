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
        themes: ['garden', 'dim'],
    },
}

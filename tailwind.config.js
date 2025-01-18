/** @type {import('tailwindcss').Config} */
module.exports = {
    content: ["./**/*.{html,razor}"],
    purge: {
        enabled: true,
        content: ["./**/*.{html,razor}"],
    },
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

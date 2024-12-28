import typescript from '@rollup/plugin-typescript';
import terser from '@rollup/plugin-terser';

const isDevelopment = (process.env.NODE_ENV || '').trim() === 'Debug';

export default {
    input: {
        'SchulCloud.Frontend': './Scripts/App.ts'
    },
    output: [
        {
            name: 'SchulCloud',
            dir: './wwwroot/_content/SchulCloud.Frontend/',
            entryFileNames: '[name].min.js',
            format: 'iife',
            sourcemap: isDevelopment,
            plugins: [terser()]
        }
    ],
    plugins: [typescript({
        tsconfig: './tsconfig.json'
    })]
};

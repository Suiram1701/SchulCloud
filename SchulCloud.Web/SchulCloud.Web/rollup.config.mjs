import typescript from '@rollup/plugin-typescript';
import terser from '@rollup/plugin-terser';

const outputPath = './wwwroot/dist/js';

export default {
    input: {
        'Main': './Scripts/Main.ts'
    },
    output: [
        {
            name: 'SchulCloud',
            dir: outputPath,
            entryFileNames: '[name].min.js',
            format: 'iife',
            sourcemap: true,     // As an Aspire hostet project is it currently not possible to use ts code debugging so there is no need for a source map.
            plugins: [terser()]
        }
    ],
    plugins: [typescript({
        tsconfig: './tsconfig.json'
    })]
};

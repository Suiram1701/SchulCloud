import typescript from '@rollup/plugin-typescript';
import terser from '@rollup/plugin-terser';

const outputPath = './wwwroot/dist/js';

export default {
    input: {
        'AppBundle': './Scripts/Main.ts'
    },
    output: [
        {
            name: 'SchulCloud',
            dir: outputPath,
            entryFileNames: '[name].min.js',
            format: 'iife',
            sourcemap: true,
            plugins: [terser()]
        }
    ],
    plugins: [typescript({
        tsconfig: './tsconfig.json'
    })]
};

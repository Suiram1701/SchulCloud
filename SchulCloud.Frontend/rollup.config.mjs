import typescript from '@rollup/plugin-typescript';
import terser from '@rollup/plugin-terser';

const outputDir = 'wwwroot/_content';
const isDevelopment = (process.env.NODE_ENV || '').trim() === 'Debug';

export default {
    input: {
        'Frontend': 'Scripts/Frontend.ts'
    },
    output: [
        {
            name: 'SchulCloud',
            dir: outputDir,
            entryFileNames: '[name].min.js',
            format: 'iife',
            sourcemap: isDevelopment,
            plugins: [terser()]
        }
    ],
    plugins: [typescript({
        tsconfig: 'tsconfig.json'
    })]
};

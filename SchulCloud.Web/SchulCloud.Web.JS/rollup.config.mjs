import typescript from '@rollup/plugin-typescript';
import terser from '@rollup/plugin-terser';

const outputPath = './../SchulCloud.Web/wwwroot/dist';

export default {
    input: {
        'schulcloud.web': './src/main.ts'
    },
    output: [
        {
            dir: outputPath,
            format: 'iife',
            name: '__schulCloudJS',
            entryFileNames: '[name].min.js',
            plugins: [terser()],
            sourcemap: true
        }
    ],
    plugins: [typescript()]
};

const fs = require('fs');
const theme = require('antd/lib/theme').default;
const layout = require('antd/lib/layout/style').prepareComponentToken;
const result = { version: require('antd/package.json').version, method: 'Official getDesignToken + Layout prepareComponentToken', modes: {} };
for (const mode of ['Light', 'Dark']) {
 const token = theme.getDesignToken({ algorithm: mode === 'Light' ? theme.defaultAlgorithm : theme.darkAlgorithm });
 result.modes[mode] = { component: layout(token), borderRadiusLG: token.borderRadiusLG, fontSize: token.fontSize, lineHeight: token.lineHeight };
}
fs.writeFileSync(process.argv[2], JSON.stringify(result, null, 2));
console.log(JSON.stringify(result));

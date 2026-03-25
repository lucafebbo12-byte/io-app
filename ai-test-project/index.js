const express = require('express');

const app = express();
const PORT = 3000;

app.get('/', (req, res) => {
  res.send('Homepage is running.');
});

app.get('/hello', (req, res) => {
  res.json({ message: 'Hello from Express API!' });
});

app.listen(PORT, () => {
  console.log(`Server running at http://localhost:${PORT}`);
});

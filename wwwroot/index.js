async function fetchBlocks() {
  const response = await fetch('/blocks');
  const data = await response.json();
  document.getElementById('result').innerText = JSON.stringify(data, null, 2);
}

async function searchBlock() {
  const data = prompt('Enter block data to search for:');
  if (data) {
      const response = await fetch(`/search?data=${encodeURIComponent(data)}`);
      const result = await response.json();
      document.getElementById('result').innerText = JSON.stringify(result, null, 2);
  }
}

async function createBlock() {
  const data = prompt('Enter block data to create:');
  if (data) {
      const response = await fetch('/create', {
          method: 'POST',
          headers: {
              'Content-Type': 'application/json',
          },
          body: JSON.stringify(data), // Adjust based on your Block model
      });
      const result = await response.text();
      document.getElementById('result').innerText = result;
  }
}
async function fetchBlocks() {
  const response = await fetch('/blocks');
  const data = await response.json();
  console.log(data);
  const blocksContainer = document.getElementById('blocks');
  blocksContainer.innerHTML = null;

  data.forEach(block => {
    const blockDiv = document.createElement('div');
    blockDiv.classList.add('block');

    for (const key in block) {
      const para = document.createElement('p');
      if (key === 'data') {
        para.innerHTML = `<strong>${key}:</strong> <div class="data-block">${block[key]}</div>`;
      }
      else {
        para.innerHTML = `<strong>${key}:</strong> ${block[key]}`;
      }
      blockDiv.appendChild(para);
    }

    blocksContainer.appendChild(blockDiv);
  });
}


async function searchBlock() {
  const data = prompt('Enter block data to search for:');
  if (data) {
      const response = await fetch(`/searchData?data=${encodeURIComponent(data)}`);
      const result = await response.json();
      document.getElementById('result').innerText = JSON.stringify(result, null, 2);
  }
}

async function searchBlockByHash() {
  const hash = prompt('Enter block hash to search for:');
  if (hash) {
      const response = await fetch(`/searchHash?hash=${encodeURIComponent(hash)}`);
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
      fetchBlocks();
  }
}


fetchBlocks();
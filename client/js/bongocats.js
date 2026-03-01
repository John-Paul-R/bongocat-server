function createBongoCatImages(username) {
  const catsContainer = document.getElementById("bongocat-container");
  function configureCatImage(imageElement, src) {
    imageElement.src = src;
  }
  const thisCatContainer = catsContainer.appendChild(
    document.createElement("div"),
  );
  thisCatContainer.classList.add("cat");
  const bongoElements = {
    neither: thisCatContainer.appendChild(document.createElement("img")),
    left: thisCatContainer.appendChild(document.createElement("img")),
    right: thisCatContainer.appendChild(document.createElement("img")),
    both: thisCatContainer.appendChild(document.createElement("img")),
  };
  configureCatImage(bongoElements.neither, "assets/bongo-cat-both-up.png");
  configureCatImage(bongoElements.left, "assets/bongo-cat-left-down.png");
  configureCatImage(bongoElements.right, "assets/bongo-cat-right-down.png");
  configureCatImage(bongoElements.both, "assets/bongo-cat-both-down.png");
  bongoElements.left.style.display = "none";
  bongoElements.right.style.display = "none";
  bongoElements.both.style.display = "none";

  const nameDiv = thisCatContainer.appendChild(document.createElement("div"));
  nameDiv.classList.add("cat-name");
  nameDiv.textContent = username;

  return bongoElements;
}

class BongoCat {
  username;
  bongoElements;

  bongoRestoreTimeout = {
    left: null,
    right: null,
  };

  constructor(username) {
    this.username = username;
    this.bongoElements = createBongoCatImages(username);
  }

  /**
   * @param {'left'|'right'} direction
   */
  bongo(direction) {
    const isLeftVisuallyBongoed =
      direction === "left" || this.bongoRestoreTimeout.left;
    const isRightVisuallyBongoed =
      direction === "right" || this.bongoRestoreTimeout.right;
    if (this.bongoRestoreTimeout[direction]) {
      clearTimeout(this.bongoRestoreTimeout[direction]);
    }
    const cat = this;
    this.bongoRestoreTimeout[direction] = setTimeout(() => {
      this.bongoRestoreTimeout[direction] = null;

      // the direction that was triggered will not count for the
      // "what to do next" ,as its been disabled, so exclude it.
      const isLeftVisuallyBongoed =
        direction !== "left" && this.bongoRestoreTimeout.left;
      const isRightVisuallyBongoed =
        direction !== "right" && this.bongoRestoreTimeout.right;
      this.displayBongo(
        isLeftVisuallyBongoed && isRightVisuallyBongoed
          ? "both"
          : isLeftVisuallyBongoed
            ? "left"
            : isRightVisuallyBongoed
              ? "right"
              : "neither",
      );
    }, 100);

    this.displayBongo(
      isLeftVisuallyBongoed && isRightVisuallyBongoed
        ? "both"
        : isLeftVisuallyBongoed
          ? "left"
          : isRightVisuallyBongoed
            ? "right"
            : "neither",
    );
  }

  /**
   * @param {'left' | 'right' | 'both' | 'neither'} frame
   */
  displayBongo(frame) {
    this.bongoElements.neither.style.display = "none";
    this.bongoElements.left.style.display = "none";
    this.bongoElements.right.style.display = "none";
    this.bongoElements.both.style.display = "none";
    this.bongoElements[frame].style.display = "block";
  }
}

/** @type {{ [string]: BongoCat }} */
const bongoCats = {};

const bongo_data_pattern =
  /(?:\[(.+?)\])? key pressed \((right|left)\) at (\d+)/;
export function handleBongoMessage(data) {
  console.log("raw message", data);
  const match = bongo_data_pattern.exec(data);
  if (match) {
    const username = match[1];
    const direction = match[2];
    const timestamp = match[3];
    console.log(`Bongo cat '${username}' pressed ${direction} at ${timestamp}`);
    if (!bongoCats[username]) {
      bongoCats[username] = new BongoCat(username);
    }
    bongoCats[username].bongo(direction);
  }
}

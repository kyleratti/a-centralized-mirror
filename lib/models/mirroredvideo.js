"use strict";
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};
Object.defineProperty(exports, "__esModule", { value: true });
const sequelize_typescript_1 = require("sequelize-typescript");
const _1 = require(".");
let MirroredVideo = class MirroredVideo extends sequelize_typescript_1.Model {
};
__decorate([
    sequelize_typescript_1.Column({
        allowNull: false,
        unique: false
    }),
    __metadata("design:type", String)
], MirroredVideo.prototype, "url", void 0);
__decorate([
    sequelize_typescript_1.ForeignKey(() => _1.CommentReply),
    __metadata("design:type", Number)
], MirroredVideo.prototype, "commentId", void 0);
__decorate([
    sequelize_typescript_1.BelongsTo(() => _1.CommentReply),
    __metadata("design:type", _1.CommentReply)
], MirroredVideo.prototype, "comment", void 0);
__decorate([
    sequelize_typescript_1.ForeignKey(() => _1.RegisteredBot),
    __metadata("design:type", Number)
], MirroredVideo.prototype, "botId", void 0);
__decorate([
    sequelize_typescript_1.BelongsTo(() => _1.RegisteredBot),
    __metadata("design:type", _1.RegisteredBot)
], MirroredVideo.prototype, "bot", void 0);
__decorate([
    sequelize_typescript_1.CreatedAt,
    __metadata("design:type", Date)
], MirroredVideo.prototype, "createdAt", void 0);
__decorate([
    sequelize_typescript_1.UpdatedAt,
    __metadata("design:type", Date)
], MirroredVideo.prototype, "updatedAt", void 0);
__decorate([
    sequelize_typescript_1.DeletedAt,
    __metadata("design:type", Date)
], MirroredVideo.prototype, "deletedAt", void 0);
MirroredVideo = __decorate([
    sequelize_typescript_1.Table({
        timestamps: true
    })
], MirroredVideo);
exports.MirroredVideo = MirroredVideo;
//# sourceMappingURL=mirroredvideo.js.map